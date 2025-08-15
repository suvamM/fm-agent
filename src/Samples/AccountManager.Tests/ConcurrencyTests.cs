using System;
using System.Threading.Tasks;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote;
using Samples.AccountManager;
using FluentAssertions;

namespace AccountManager.Tests
{
    public class ConcurrencyTests
    {
        [Microsoft.Coyote.SystematicTesting.Test]
        public async Task TestConcurrentCreateAccount()
        {
            var db = new InMemoryDbCollection();
            var manager = new Samples.AccountManager.AccountManager(db);

            Task task1 = manager.CreateAccount("user1", "payload1");
            Task task2 = manager.CreateAccount("user1", "payload2");

            await Task.WhenAll(task1, task2);
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public async Task TestConcurrentGetAccount()
        {
            var db = new InMemoryDbCollection();
            var manager = new Samples.AccountManager.AccountManager(db);

            await manager.CreateAccount("user1", "payload1");

            Task<string> task1 = manager.GetAccount("user1");
            Task<string> task2 = manager.GetAccount("user1");

            var results = await Task.WhenAll(task1, task2);
            results[0].Should().Be("payload1");
            results[1].Should().Be("payload1");
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public async Task TestConcurrentDeleteAccount()
        {
            var db = new InMemoryDbCollection();
            var manager = new Samples.AccountManager.AccountManager(db);

            await manager.CreateAccount("user1", "payload1");

            Task task1 = manager.DeleteAccount("user1");
            Task task2 = manager.DeleteAccount("user1");

            await Task.WhenAll(task1, task2);
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public async Task TestConcurrentCreateRow()
        {
            var db = new InMemoryDbCollection();

            Task task1 = db.CreateRow("row1", "value1");
            Task task2 = db.CreateRow("row1", "value2");

            await Task.WhenAll(task1, task2);
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public async Task TestConcurrentDoesRowExist()
        {
            var db = new InMemoryDbCollection();

            await db.CreateRow("row1", "value1");

            Task<bool> task1 = db.DoesRowExist("row1");
            Task<bool> task2 = db.DoesRowExist("row1");

            var results = await Task.WhenAll(task1, task2);
            results[0].Should().BeTrue();
            results[1].Should().BeTrue();
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public async Task TestConcurrentGetRow()
        {
            var db = new InMemoryDbCollection();

            await db.CreateRow("row1", "value1");

            Task<string> task1 = db.GetRow("row1");
            Task<string> task2 = db.GetRow("row1");

            var results = await Task.WhenAll(task1, task2);
            results[0].Should().Be("value1");
            results[1].Should().Be("value1");
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public async Task TestConcurrentDeleteRow()
        {
            var db = new InMemoryDbCollection();

            await db.CreateRow("row1", "value1");

            Task task1 = db.DeleteRow("row1");
            Task task2 = db.DeleteRow("row1");

            await Task.WhenAll(task1, task2);
        }
    }
}