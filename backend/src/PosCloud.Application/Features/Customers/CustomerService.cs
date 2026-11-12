using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Customers.Dtos;
using PosCloud.Application.Features.Loyalty;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Customers;

public class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _db;

    public CustomerService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Customers
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return list.Select(CustomerDto.FromEntity).ToList();
    }

    public async Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var c = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Mijoz topilmadi: {id}");
        return CustomerDto.FromEntity(c);
    }

    public async Task<CustomerDto?> FindByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        var c = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Phone == phone, cancellationToken);
        return c is null ? null : CustomerDto.FromEntity(c);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            DiscountPercent = request.DiscountPercent,
            Comment = request.Comment?.Trim()
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);

        return CustomerDto.FromEntity(customer);
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Mijoz topilmadi: {id}");

        customer.Name = request.Name.Trim();
        customer.Phone = request.Phone?.Trim();
        customer.Email = request.Email?.Trim();
        customer.Address = request.Address?.Trim();
        customer.DiscountPercent = request.DiscountPercent;
        customer.Comment = request.Comment?.Trim();
        customer.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return CustomerDto.FromEntity(customer);
    }

    public async Task<CustomerDto> AdjustPointsAsync(Guid id, AdjustPointsRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Mijoz topilmadi: {id}");

        // Qo'lda tuzatish (Adjust). Manfiy tuzatish balansdan oshsa — 0 gacha.
        LoyaltyRules.Apply(_db, customer, request.Points, LoyaltyTransactionType.Adjust, LoyaltySource.Store, 0,
            request.Comment);
        await _db.SaveChangesAsync(cancellationToken);

        return CustomerDto.FromEntity(customer);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Mijoz topilmadi: {id}");

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
