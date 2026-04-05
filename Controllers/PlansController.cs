using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportLife.Data;
using SportLife.DTOs;
using SportLife.Entities;

namespace SportLife.Controllers;

[ApiController]
[Route("api/plans")]
public class PlansController : ControllerBase
{
    private readonly AppDbContext _db;

    public PlansController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var plans = await _db.MembershipPlans
            .Where(p => p.Active)
            .OrderBy(p => p.Price)
            .Select(p => new PlanDto(p.Id, p.Name, p.Price, p.DurationDays, p.Active))
            .ToListAsync();
        return Ok(plans);
    }

    [HttpGet("all")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllAdmin()
    {
        var plans = await _db.MembershipPlans
            .OrderBy(p => p.Price)
            .Select(p => new PlanDto(p.Id, p.Name, p.Price, p.DurationDays, p.Active))
            .ToListAsync();
        return Ok(plans);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _db.MembershipPlans.FindAsync(id);
        if (plan == null) return NotFound();
        return Ok(new PlanDto(plan.Id, plan.Name, plan.Price, plan.DurationDays, plan.Active));
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreatePlanRequest req)
    {
        var plan = new MembershipPlan
        {
            Name = req.Name,
            Price = req.Price,
            DurationDays = req.DurationDays
        };
        _db.MembershipPlans.Add(plan);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = plan.Id },
            new PlanDto(plan.Id, plan.Name, plan.Price, plan.DurationDays, plan.Active));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlanRequest req)
    {
        var plan = await _db.MembershipPlans.FindAsync(id);
        if (plan == null) return NotFound();

        if (req.Name != null) plan.Name = req.Name;
        if (req.Price.HasValue) plan.Price = req.Price.Value;
        if (req.DurationDays.HasValue) plan.DurationDays = req.DurationDays.Value;
        if (req.Active.HasValue) plan.Active = req.Active.Value;

        await _db.SaveChangesAsync();
        return Ok(new PlanDto(plan.Id, plan.Name, plan.Price, plan.DurationDays, plan.Active));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var plan = await _db.MembershipPlans.FindAsync(id);
        if (plan == null) return NotFound();
        plan.Active = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
