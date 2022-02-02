using System;
using System.Diagnostics;
using NUnit.Framework;
using Rhino.Mocks;

namespace ServiceStack.Logging.Tests.Support
{
    public class TestBase
    {
        private MockRepository mocks;

        protected virtual MockRepository Mocks
        {
            get { return mocks; }
        }

        [SetUp]
        protected virtual void SetUp()
        {
            mocks = new MockRepository();
        }

        [TearDown]
        protected virtual void TearDown()
        {
            mocks = null;
        }

        protected virtual void ReplayAll()
        {
            Mocks.ReplayAll();
        }

        protected virtual void VerifyAll()
        {
            try
            {
                Mocks.VerifyAll();
            }
            catch (InvalidOperationException ex)
            {
                Debug.Print("InvalidOperationException thrown: {0}", ex.Message);
            }
        }
    }
}