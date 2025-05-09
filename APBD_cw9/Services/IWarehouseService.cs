using APBD_cw9.Models.DTOs;

namespace APBD_cw9.Services;

public interface IWarehouseService
{
    Task<int> AddProductManuallyAsync(AddProductDTO request);
    Task<int> AddProductWithProcedureAsync(AddProductDTO request);
}