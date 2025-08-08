using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusSales.Core.Models;

namespace NexusSales.Tests.Core
{
    [TestClass]
    public class UserAccountTests
    {
        [TestMethod]
        public void CanCreateUserAccount()
        {
            var user = new UserAccount { FacebookId = "123", Name = "Test", PhoneNumber = "555-1234" };
            Assert.AreEqual("123", user.FacebookId);
        }
    }
}