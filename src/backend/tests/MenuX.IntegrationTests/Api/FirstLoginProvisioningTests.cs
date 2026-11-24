using MenuX.Identity.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace MenuX.IntegrationTests.Api;

/// <summary>
/// First-login user creation (product owner, 2026-11-23): email, verified flag and name are read from the ID
/// token once; afterwards the database is the source of truth. API-side email verification enforcement.
/// </summary>
public sealed class FirstLoginProvisioningTests(PostgresFixture db) : IDisposable
{
    private readonly ApiFactory _api = new(db);

    public void Dispose() => _api.Dispose();

    private Task<ProvisioningResult> EnsureAsync(FirstLoginProfile profile)
    {
        var scope = _api.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IIdentityApi>().EnsureUserAsync(profile, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Unverified_email_creates_no_user()
    {
        var subject = Guid.NewGuid().ToString();
        var result = await EnsureAsync(new FirstLoginProfile(ApiFactory.Issuer, subject, $"{subject}@example.test", EmailVerified: false, "Una"));
        Assert.Equal(ProvisioningOutcome.EmailNotVerified, result.Outcome);
        Assert.Null(await FindAsync(subject));
    }

    [Fact]
    public async Task Missing_email_creates_no_user()
    {
        var result = await EnsureAsync(new FirstLoginProfile(ApiFactory.Issuer, Guid.NewGuid().ToString(), null, EmailVerified: true, "Nobody"));
        Assert.Equal(ProvisioningOutcome.EmailNotVerified, result.Outcome);
    }

    [Fact]
    public async Task First_login_creates_the_user_and_later_logins_dont_overwrite_the_profile()
    {
        var subject = Guid.NewGuid().ToString();
        var first = await EnsureAsync(new FirstLoginProfile(ApiFactory.Issuer, subject, $"{subject}@example.test", true, "Original Name"));
        Assert.Equal(ProvisioningOutcome.Created, first.Outcome);

        var again = await EnsureAsync(new FirstLoginProfile(ApiFactory.Issuer, subject, "changed@example.test", true, "Changed Name"));
        Assert.Equal(ProvisioningOutcome.Existing, again.Outcome);
        Assert.Equal(first.UserId, again.UserId);
        Assert.Equal(("Original Name", $"{subject}@example.test", true), await FindAsync(subject));
    }

    [Fact]
    public async Task An_email_already_used_by_another_identity_is_rejected()
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        Assert.Equal(ProvisioningOutcome.Created, (await EnsureAsync(new FirstLoginProfile(ApiFactory.Issuer, Guid.NewGuid().ToString(), email, true, "A"))).Outcome);
        Assert.Equal(ProvisioningOutcome.EmailInUse, (await EnsureAsync(new FirstLoginProfile("https://other.idp", Guid.NewGuid().ToString(), email, true, "B"))).Outcome);
    }

    [Fact]
    public async Task The_database_refuses_an_active_user_without_a_verified_email()
    {
        var id = Guid.CreateVersion7();
        await using var connection = new NpgsqlConnection(db.App());
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var tx = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await using (var set = new NpgsqlCommand($"SELECT set_config('app.user_id', '{id}', true)", connection, tx))
        {
            await set.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await using var insert = new NpgsqlCommand($"""
            INSERT INTO identity.users (id, idp_issuer, idp_subject, email, display_name, status)
            VALUES ('{id}', 'https://x', '{id}', '{id:N}@example.test', 'X', 'active')
            """, connection, tx);
        var error = await Assert.ThrowsAsync<PostgresException>(() => insert.ExecuteNonQueryAsync(TestContext.Current.CancellationToken));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
    }

    private async Task<(string Name, string Email, bool Verified)?> FindAsync(string subject)
    {
        // Platform-level check through the sign-in lookup + the user's own row.
        await using var connection = new NpgsqlConnection(db.App());
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var find = new NpgsqlCommand($"SELECT identity.find_user_id('{ApiFactory.Issuer}', '{subject}')", connection);
        if (await find.ExecuteScalarAsync(TestContext.Current.CancellationToken) is not Guid id)
        {
            return null;
        }

        await using var tx = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await using (var set = new NpgsqlCommand($"SELECT set_config('app.user_id', '{id}', true)", connection, tx))
        {
            await set.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await using var read = new NpgsqlCommand($"SELECT display_name, email, email_verified_at IS NOT NULL FROM identity.users WHERE id = '{id}'", connection, tx);
        await using var reader = await read.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        await reader.ReadAsync(TestContext.Current.CancellationToken);
        return (reader.GetString(0), reader.GetString(1), reader.GetBoolean(2));
    }
}
