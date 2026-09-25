using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMusic.API.Contracts;
using MyMusic.Core.Services;

namespace MyMusic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArtistController(IArtistService artists) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ArtistResponse>> GetAll(CancellationToken cancellationToken) =>
        (await artists.GetAllAsync(cancellationToken)).Select(ArtistResponse.From);

    [HttpGet("{id}")]
    public async Task<ActionResult<ArtistResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var artist = await artists.GetByIdAsync(id, cancellationToken);
        return artist is null ? NotFound() : ArtistResponse.From(artist);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ArtistResponse>> Create(SaveArtistRequest request, CancellationToken cancellationToken)
    {
        var artist = await artists.CreateAsync(request.Name, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = artist.Id }, ArtistResponse.From(artist));
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<ArtistResponse>> Update(string id, SaveArtistRequest request, CancellationToken cancellationToken)
    {
        var result = await artists.RenameAsync(id, request.Name, cancellationToken);
        return result.Status == ServiceStatus.Success ? ArtistResponse.From(result.Value!) : this.ToProblem(result);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var result = await artists.DeleteAsync(id, cancellationToken);
        return result.Status == ServiceStatus.Success ? NoContent() : this.ToProblem(result);
    }
}
