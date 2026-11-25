using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using LagoVista.Core.Security;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.Web.Common.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace LagoVista.IoT.Web.Common.Tests
{
    [TestClass]
    public class SignedRequestBodyIntegrityTests
    {
        [TestMethod]
        public async Task MatchingBytesAndEmptyBodyAreAcceptedAndStreamRewound()
        {
            foreach (var bytes in new[] { Array.Empty<byte>(), new byte[] { 1, 2, 3, 255 } })
            {
                var request = Request(bytes, bytes);
                var result = await Validator().ValidateServiceHttpV1Async(request);
                Assert.IsTrue(result.Successful);
                Assert.AreEqual(0L, request.Body.Position);
                using var copy = new MemoryStream();
                await request.Body.CopyToAsync(copy);
                CollectionAssert.AreEqual(bytes, copy.ToArray());
            }
        }

        [TestMethod]
        public async Task MismatchAndMalformedDigestAreRejected()
        {
            var request = Request(new byte[] { 1 }, new byte[] { 2 });
            var result = await Validator().ValidateServiceHttpV1Async(request);
            Assert.AreEqual("signed_body_hash_mismatch", result.ErrorCode);
            Assert.AreEqual(0L, request.Body.Position);
            request.Headers[SignedRequestHeaders.BodySha256] = "not-base64";
            result = await Validator().ValidateServiceHttpV1Async(request);
            Assert.AreEqual("signed_body_hash_mismatch", result.ErrorCode);
        }

        [TestMethod]
        public async Task BadSignatureRemainsRejectedBeforeBodyVerification()
        {
            var request = Request(new byte[] { 1 }, new byte[] { 2 });
            var result = await Validator(false).ValidateServiceHttpV1Async(request);
            Assert.AreEqual("invalid_signature", result.ErrorCode);
            Assert.AreEqual(0L, request.Body.Position);
        }

        private static HttpRequest Request(byte[] actual, byte[] signed)
        {
            var request = new DefaultHttpContext().Request;
            request.Method = "POST";
            request.Path = "/api/test";
            request.Body = new MemoryStream(actual);
            request.Headers[SignedRequestHeaders.BodySha256] = Convert.ToBase64String(SHA256.HashData(signed));
            return request;
        }

        private static SignedRequestHttpValidator Validator(bool valid = true)
        {
            var signature = new Mock<ISignedRequestValidatorService>();
            signature.Setup(s => s.ValidateServiceHttpV1Async(It.IsAny<SignedRequestValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(valid ? SignedRequestValidationResult.Success("request", "key") :
                    SignedRequestValidationResult.FromError("invalid_signature", "Invalid signature"));
            return new SignedRequestHttpValidator(signature.Object, Mock.Of<IAdminLogger>(), Mock.Of<IRuntimeSignedRequestSessionService>());
        }
    }
}
