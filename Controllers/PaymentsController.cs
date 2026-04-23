using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLife.Data;
using SportLife.DTOs;
using SportLife.Entities;
using SportLife.Services;
using System.Security.Claims;

namespace SportLife.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPaymentService _paymentService;
    private readonly IMembershipService _membershipService;

    public PaymentsController(AppDbContext db, IPaymentService paymentService, IMembershipService membershipService)
    {
        _db = db;
        _paymentService = paymentService;
        _membershipService = membershipService;
    }

    [HttpGet("public-config")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicConfig([FromServices] IConfiguration config)
    {
        var dbConfig = await _db.MercadoPagoConfigs.FirstOrDefaultAsync();
        var publicKey = dbConfig?.PublicKey ?? config["MercadoPago:PublicKey"] ?? "";
        var isSandbox = dbConfig?.IsTestMode ?? config.GetValue<bool>("MercadoPago:Sandbox");
        return Ok(new { publicKey, isSandbox });
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _db.Payments
            .Include(p => p.User)
            .Include(p => p.Plan)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto(
                p.Id, p.UserId, p.User!.Name, p.PlanId, p.Plan!.Name,
                p.MercadoPagoPaymentId, p.PreferenceId, p.Status, p.Amount,
                p.CreatedAt, p.ApprovedAt))
            .ToListAsync();
        return Ok(payments);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var payments = await _db.Payments
            .Include(p => p.Plan)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto(
                p.Id, p.UserId, "", p.PlanId, p.Plan!.Name,
                p.MercadoPagoPaymentId, p.PreferenceId, p.Status, p.Amount,
                p.CreatedAt, p.ApprovedAt))
            .ToListAsync();
        return Ok(payments);
    }

    [HttpPost("preference")]
    public async Task<IActionResult> CreatePreference([FromBody] CreatePaymentPreferenceRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userEmail = User.FindFirstValue(ClaimTypes.Email)!;

        var plan = await _db.MembershipPlans.FindAsync(req.PlanId);
        if (plan == null || !plan.Active)
            return BadRequest(new { message = "Plan not found or inactive" });

        // Create pending payment record
        var payment = new Payment
        {
            UserId = userId,
            PlanId = req.PlanId,
            Amount = plan.Price,
            Status = "pending"
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        var prefRequest = new CreatePreferenceRequest(
            Title: plan.Name,
            Amount: plan.Price,
            PlanId: req.PlanId,
            UserId: userId,
            UserEmail: userEmail
        );

        try
        {
            var preference = await _paymentService.CreatePreference(prefRequest);
            payment.PreferenceId = preference.PreferenceId;
            await _db.SaveChangesAsync();
            return Ok(preference);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("process")]
    public async Task<IActionResult> Process([FromBody] ProcessBrickPaymentRequest req)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var userEmail = User.FindFirstValue(ClaimTypes.Email)!;

        var plan = await _db.MembershipPlans.FindAsync(req.PlanId);
        if (plan == null || !plan.Active)
            return BadRequest(new { message = "Plan no encontrado o inactivo" });

        var paymentRequest = new ProcessPaymentRequest(
            Token: req.Token,
            PaymentMethodId: req.PaymentMethodId,
            Installments: req.Installments,
            IssuerId: req.IssuerId,
            Amount: plan.Price,
            Email: userEmail,
            IdentificationType: req.IdentificationType,
            IdentificationNumber: req.IdentificationNumber,
            ExternalReference: $"{userId}|{req.PlanId}",
            Description: plan.Name
        );

        try
        {
            var result = await _paymentService.ProcessPayment(paymentRequest);

            if (result.Status == "approved")
                await _membershipService.ActivateMembership(userId, req.PlanId, result.MercadoPagoPaymentId);

            return Ok(new { status = result.Status, paymentId = result.MercadoPagoPaymentId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
