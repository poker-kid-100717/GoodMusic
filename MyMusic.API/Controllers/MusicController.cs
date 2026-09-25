using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMusic.API.Contracts;
using MyMusic.Core.Services;

namespace MyMusic.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MusicController(IMusicService musics) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<MusicResponse>> GetAll(CancellationToken cancellationToken) =>
        (await musics.GetAllAsync(cancellationToken)).Select(MusicResponse.From);

    [HttpGet("{id}")]
    public async Task<ActionResult<MusicResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var music = await musics.GetByIdAsync(id, cancellationToken);
        return music is null ? NotFound() : MusicResponse.From(music);
    }

    [HttpGet("artist/{artistId}")]
    public async Task<IEnumerable<MusicResponse>> GetByArtist(string artistId, CancellationToken cancellationToken) =>
        (await musics.GetByArtistIdAsync(artistId, cancellationToken)).Select(MusicResponse.From);

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<MusicResponse>> Create(SaveMusicRequest request, CancellationToken cancellationToken)
    {
        var result = await musics.CreateAsync(request.Name, request.ArtistId, cancellationToken);
        return result.Status == ServiceStatus.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, MusicResponse.From(result.Value))
            : this.ToProblem(result);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<MusicResponse>> Update(string id, SaveMusicRequest request, CancellationToken cancellationToken)
    {
        var result = await musics.UpdateAsync(id, request.Name, request.ArtistId, cancellationToken);
        return result.Status == ServiceStatus.Success ? MusicResponse.From(result.Value!) : this.ToProblem(result);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken) =>
        await musics.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
