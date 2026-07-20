# Good and Bad Tests

## Good Tests

**Integration-style**: Test through real interfaces, not mocks of internal parts.

```csharp
// GOOD: Tests observable behavior
[Fact]
public async Task UserCanCheckoutWithValidCart()
{
    var cart = CreateCart();
    cart.Add(product);
    var result = await Checkout(cart, paymentMethod);
    Assert.Equal(OrderStatus.Confirmed, result.Status);
}
```

Characteristics:

- Tests behavior users/callers care about
- Uses public API only
- Survives internal refactors
- Describes WHAT, not HOW
- One logical assertion per test

## Bad Tests

**Implementation-detail tests**: Coupled to internal structure.

```csharp
// BAD: Tests implementation details
[Fact]
public async Task CheckoutCallsPaymentServiceProcess()
{
    var mockPayment = new Mock<IPaymentService>();
    await Checkout(cart, mockPayment.Object);
    mockPayment.Verify(p => p.Process(cart.Total), Times.Once);
}
```

Red flags:

- Mocking internal collaborators
- Testing private methods
- Asserting on call counts/order
- Test breaks when refactoring without behavior change
- Test name describes HOW not WHAT
- Verifying through external means instead of interface

```csharp
// BAD: Bypasses interface to verify
[Fact]
public async Task CreateUserSavesToDatabase()
{
    await CreateUser(new UserRequest { Name = "Alice" });
    var row = await Db.QuerySingleAsync("SELECT * FROM Users WHERE Name = @Name", new { Name = "Alice" });
    Assert.NotNull(row);
}

// GOOD: Verifies through interface
[Fact]
public async Task CreateUserMakesUserRetrievable()
{
    var user = await CreateUser(new UserRequest { Name = "Alice" });
    var retrieved = await GetUser(user.Id);
    Assert.Equal("Alice", retrieved.Name);
}
```

**Tautological tests**: Expected value restates the implementation, so the test passes by construction.

```csharp
// BAD: Expected value is recomputed the way the code computes it
[Fact]
public void CalculateTotalSumsLineItems()
{
    var items = new[] { new LineItem(10), new LineItem(5) };
    var expected = items.Sum(i => i.Price);
    Assert.Equal(expected, CalculateTotal(items));
}

// GOOD: Expected value is an independent, known literal
[Fact]
public void CalculateTotalSumsLineItems()
{
    Assert.Equal(15, CalculateTotal(new[] { new LineItem(10), new LineItem(5) }));
}
```
