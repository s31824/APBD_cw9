using APBD_cw9.Models.DTOs;
using APBD_cw9.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD_cw9.Controllers;

[ApiController]
[Route("api/warehouse")]
public class WarehouseController : ControllerBase
{
        private readonly IWarehouseService _service;

        public WarehouseController(IWarehouseService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> AddProductManually([FromBody] AddProductDTO request)
        {
            try
            {
                var newId = await _service.AddProductManuallyAsync(request);
                return Created($"api/warehouse/{newId}", new { IdProductWarehouse = newId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }


        [HttpPost("procedure")]
        public async Task<IActionResult> AddProductWithProcedure([FromBody] AddProductDTO request)
        {
            try
            {
                var newId = await _service.AddProductWithProcedureAsync(request);
                return Created($"api/warehouse/{newId}", new { IdProductWarehouse = newId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }
        
}


