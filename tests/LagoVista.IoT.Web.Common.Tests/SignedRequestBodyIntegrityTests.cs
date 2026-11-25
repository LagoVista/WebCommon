using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using LagoVista.Core.Security;
using LagoVista.CloudStorage.RuntimeControls;
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

        [TestMethod]
        public async Task TemporaryRuntimeRollbackDoesNotBypassSignatureValidation()
        {
            var control = new Mock<IRuntimeControlService>();
            var enabled = true;
            control.Setup(x => x.IsEnabled("Security.SignedRequests.VerifyBodyHash")).Returns(() => enabled);
            var request = Request(new byte[] { 1 }, new byte[] { 2 });
            var validator = Validator(controls: control.Object);
            Assert.AreEqual("signed_body_hash_mismatch", (await validator.ValidateServiceHttpV1Async(request)).ErrorCode);
            enabled = false;
            Assert.IsTrue((await validator.ValidateServiceHttpV1Async(request)).Successful);
            Assert.AreEqual("invalid_signature", (await Validator(false, control.Object).ValidateServiceHttpV1Async(request)).ErrorCode);
            enabled = true;
            Assert.AreEqual("signed_body_hash_mismatch", (await validator.ValidateServiceHttpV1Async(request)).ErrorCode);
        }

        [TestMethod]
        public async Task RealRsaSignedBodyAcceptsOriginalBytesAndRejectsAlteration()
        {
            using var rsa = RSA.Create(3072);
            var privateKey = SignedRequestRsaXmlSerializer.ToXmlString(rsa.ExportParameters(true), true);
            var publicKey = SignedRequestRsaXmlSerializer.ToXmlString(rsa.ExportParameters(false), false);
            var bytes = new byte[] { 1, 2, 3 };
            var headers = new SignedRequestHeaderBuilder().BuildHeaders(new SignedRequestHeaderBuildContext
            {
                Profile = SignedRequestCanonicalProfile.ServiceHttpV1,
                AppKey = "api", RequestId = Guid.NewGuid().ToString("N"), DateUtc = DateTimeOffset.UtcNow, Version = "1",
                SigningKeyId = "test-key", PrivateKeyMaterial = privateKey, Method = "POST", PathAndQuery = "/api/test", Body = bytes
            });
            var keySet = new SignedRequestPublicKeySet
            {
                Environment = "dev", Version = "test", GeneratedUtc = DateTimeOffset.UtcNow,
                Keys = { new SignedRequestPublicKeyEntry
                {
                    AppKey = "api", KeyId = "test-key", Algorithm = SignedRequestSignatureAlgorithms.RsaPssSha256,
                    KeyMaterialFormat = SignedRequestKeyMaterialFormats.RsaXml, PublicKeyMaterial = publicKey,
                    Status = SignedRequestValidationKeyStatuses.Active
                } }
            };
            var verifier = new SignedRequestValidatorService(new SignedRequestPublicKeySetResolver(keySet),
                Mock.Of<ISignedRequestPublicKeyRefreshService>());
            var validator = new SignedRequestHttpValidator(verifier, Mock.Of<IAdminLogger>(), Mock.Of<IRuntimeSignedRequestSessionService>());
            var request = Request(bytes, bytes);
            foreach (var header in headers) request.Headers[header.Key] = header.Value;
            Assert.IsTrue((await validator.ValidateServiceHttpV1Async(request)).Successful);
            request.Body = new MemoryStream(new byte[] { 1, 2, 4 });
            Assert.AreEqual("signed_body_hash_mismatch", (await validator.ValidateServiceHttpV1Async(request)).ErrorCode);
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

        private static SignedRequestHttpValidator Validator(bool valid = true, IRuntimeControlService controls = null)
        {
            var signature = new Mock<ISignedRequestValidatorService>();
            signature.Setup(s => s.ValidateServiceHttpV1Async(It.IsAny<SignedRequestValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(valid ? SignedRequestValidationResult.Success("request", "key") :
                    SignedRequestValidationResult.FromError("invalid_signature", "Invalid signature"));
            return new SignedRequestHttpValidator(signature.Object, Mock.Of<IAdminLogger>(), Mock.Of<IRuntimeSignedRequestSessionService>(), controls);
        }
    }
}
