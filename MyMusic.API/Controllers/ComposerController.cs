using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMusic.API.Contracts;
using MyMusic.Core.Services;

namespace MyMusic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComposerController(IComposerService composers) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ComposerResponse>> GetAll(CancellationToken cancellationToken) =>
        (await composers.GetAllAsync(cancellationToken)).Select(ComposerResponse.From);

    [HttpGet("{id}")]
    public async Task<ActionResult<ComposerResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var composer = await composers.GetByIdAsync(id, cancellationToken);
        return composer is null ? NotFound() : ComposerResponse.From(composer);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ComposerResponse>> Create(SaveComposerRequest request, CancellationToken cancellationToken)
    {
        var composer = await composers.CreateAsync(request.FirstName, request.LastName, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = composer.Id }, ComposerResponse.From(composer));
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<ComposerResponse>> Update(string id, SaveComposerRequest request, CancellationToken cancellationToken)
    {
        var composer = await composers.UpdateAsync(id, request.FirstName, request.LastName, cancellationToken);
        return composer is null ? NotFound() : ComposerResponse.From(composer);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken) =>
        await composers.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
