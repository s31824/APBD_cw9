using System.Data;
using Microsoft.Data.SqlClient;
using APBD_cw9.Models.DTOs;

namespace APBD_cw9.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IConfiguration _configuration;
    public WarehouseService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<int> AddProductManuallyAsync(AddProductDTO request)
{
    await using SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("Default"));
    await conn.OpenAsync();

    await using SqlCommand command = new SqlCommand { Connection = conn };
    SqlTransaction transaction = (SqlTransaction)await conn.BeginTransactionAsync();
    command.Transaction = transaction;

    try
    {
        
        command.CommandText = "SELECT COUNT(1) FROM Product WHERE IdProduct = @IdProduct";
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        var exists = (int)await command.ExecuteScalarAsync();
        if (exists == 0)
            throw new ArgumentException("Product does not exist.");
        command.Parameters.Clear();

        
        command.CommandText = "SELECT COUNT(1) FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        exists = (int)await command.ExecuteScalarAsync();
        if (exists == 0)
            throw new ArgumentException("Warehouse does not exist.");
        command.Parameters.Clear();

        
        command.CommandText = @"
            SELECT TOP 1 o.IdOrder, p.Price, pw.IdProductWarehouse
            FROM [Order] o
            JOIN Product p ON o.IdProduct = p.IdProduct
            LEFT JOIN Product_Warehouse pw ON o.IdOrder = pw.IdOrder
            WHERE o.IdProduct = @IdProduct AND o.Amount = @Amount
              AND o.CreatedAt < @CreatedAt";
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        int idOrder = 0;
        decimal price = 0;
        bool alreadyFulfilled = false;

        await using (var reader = await command.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                idOrder = reader.GetInt32(0);
                price = reader.GetDecimal(1);
                if (!reader.IsDBNull(2))
                {
                    alreadyFulfilled = true;
                }
            }
            else
            {
                throw new ArgumentException("No matching order found.");
            }
        }

        command.Parameters.Clear();

        if (alreadyFulfilled)
            throw new ArgumentException("This order has already been fulfilled.");

        
        command.CommandText = "UPDATE [Order] SET FulfilledAt = @CreatedAt WHERE IdOrder = @IdOrder";
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();

        
        command.CommandText = @"
            INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
            VALUES(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
            SELECT SCOPE_IDENTITY();";
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdOrder", idOrder);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@Price", price * request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        var newId = Convert.ToInt32(await command.ExecuteScalarAsync());
        await transaction.CommitAsync();
        return newId;
    }
    catch (ArgumentException ex)
    {
        await transaction.RollbackAsync();
        throw new ArgumentException(ex.Message);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

    
    public async Task<int> AddProductWithProcedureAsync(AddProductDTO request)
    {
        await using SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("Default"));
        await conn.OpenAsync();

        await using SqlCommand command = new SqlCommand("AddProductToWarehouse", conn);
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        try
        {
            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                throw new ArgumentException("Procedure did not return any result.");
            }

            return Convert.ToInt32(result);
        }
        catch (SqlException ex)
        {
            
            throw new ArgumentException(ex.Message);
        }
    }

}