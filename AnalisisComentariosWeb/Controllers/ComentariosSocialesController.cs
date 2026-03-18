
using CargaDeEncuestasInternas.Interfaces.API;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ComentariosSocialesController : ControllerBase
{
    private readonly IComentariosSocialesService _service;

    public ComentariosSocialesController(IComentariosSocialesService service)
    {
        _service = service;
    }

    // GET: api/ComentariosSociales
    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(cancellationToken);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // GET: api/ComentariosSociales/get-by-red/Instagram
    [HttpGet("get-by-red/{redSocial}")]
    public async Task<IActionResult> GetByRedSocial(
        string redSocial, CancellationToken cancellationToken)
    {
        var result = await _service.GetByRedSocialAsync(redSocial, cancellationToken);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}