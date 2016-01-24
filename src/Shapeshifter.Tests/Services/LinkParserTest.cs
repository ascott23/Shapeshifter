﻿namespace Shapeshifter.WindowsDesktop.Services
{
    using System.Linq;
    using System.Threading.Tasks;

    using Autofac;

    using Files;
    using Files.Interfaces;

    using Infrastructure.Threading.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Web;
    using Web.Interfaces;

    [TestClass]
    public class LinkParserTest: UnitTestFor<ILinkParser>
    {
        public LinkParserTest()
        {
            ExcludeFakeFor<IAsyncFilter>();
        }

        [TestMethod]
        public async Task ExtractsAllLinksFromText()
        {
            container.Resolve<IDomainNameResolver>()
             .IsValidDomainAsync(Arg.Any<string>())
             .Returns(Task.FromResult(true));

            const string text = "hello http://google.com world https://foo.com foobar blah.dk/hey/lol%20kitten.jpg lolz foobar.com www.baz.com test.net/news+list.txt?cat=pic&id=foo28";
            
            var links = 
                await systemUnderTest.ExtractLinksFromTextAsync(text);

            Assert.IsTrue(links.Contains("http://google.com"));
            Assert.IsTrue(links.Contains("https://foo.com"));
            Assert.IsTrue(links.Contains("foobar.com"));
            Assert.IsTrue(links.Contains("www.baz.com"));
            Assert.IsTrue(links.Contains("test.net/news+list.txt?cat=pic&id=foo28"));
            Assert.IsTrue(links.Contains("blah.dk/hey/lol%20kitten.jpg"));
        }

        [TestMethod]
        public async Task HasLinkReturnsFalseWhenNoLinkPresent()
        {
            const string text = "hello world";
            Assert.IsFalse(await systemUnderTest.HasLinkAsync(text));
        }

        [TestMethod]
        public async Task HasLinkReturnsTrueWithoutProtocol()
        {
            container.Resolve<IDomainNameResolver>()
             .IsValidDomainAsync("google.com")
             .Returns(Task.FromResult(true));

            const string text = "hello google.com world";
            Assert.IsTrue(await systemUnderTest.HasLinkAsync(text));
        }

        [TestMethod]
        public async Task LinkWithSubdomainIsValid()
        {
            container.Resolve<IDomainNameResolver>()
                     .IsValidDomainAsync("foo.subdomain.google.com")
                     .Returns(Task.FromResult(true));

            const string text = "http://foo.subdomain.google.com";
            Assert.IsTrue(await systemUnderTest.IsValidLinkAsync(text));
        }

        [TestMethod]
        public async Task LinkWithHttpProtocolIsValid()
        {
            container.Resolve<IDomainNameResolver>()
             .IsValidDomainAsync("google.com")
             .Returns(Task.FromResult(true));

            const string text = "http://google.com";
            Assert.IsTrue(await systemUnderTest.IsValidLinkAsync(text));
        }

        [TestMethod]
        public async Task LinkWithHttpsProtocolIsValid()
        {
            container.Resolve<IDomainNameResolver>()
             .IsValidDomainAsync("google.com")
             .Returns(Task.FromResult(true));

            const string text = "https://google.com";
            Assert.IsTrue(await systemUnderTest.IsValidLinkAsync(text));
        }

        [TestMethod]
        public async Task LinkWithParametersIsValid()
        {
            container.Resolve<IDomainNameResolver>()
             .IsValidDomainAsync("google.com")
             .Returns(Task.FromResult(true));

            const string text = "http://google.com?hello=flyp&version=1";
            Assert.IsTrue(await systemUnderTest.IsValidLinkAsync(text));
        }

        [TestMethod]
        public async Task LinkWithDirectoriesIsValid()
        {
            container.Resolve<IDomainNameResolver>()
             .IsValidDomainAsync("google.com")
             .Returns(Task.FromResult(true));

            const string text = "http://google.com/foo/bar";
            Assert.IsTrue(await systemUnderTest.IsValidLinkAsync(text));
        }

        [TestMethod]
        public void ImageLinkHasImageType()
        {
            const string text = "google.com/foo/image.png";

            container.Resolve<IFileTypeInterpreter>()
             .GetFileTypeFromFileName(text)
             .Returns(FileType.Image);

            var linkType = systemUnderTest.GetLinkType(text);
            Assert.IsTrue(linkType.HasFlag(LinkType.ImageFile));
        }

        [TestMethod]
        public void HttpLinkHasHttpType()
        {
            const string text = "http://google.com";
            var linkType = systemUnderTest.GetLinkType(text);
            Assert.IsTrue(linkType.HasFlag(LinkType.Http));
        }

        [TestMethod]
        public async Task SeveralLinksCanFindProperType()
        {
            container.Resolve<IDomainNameResolver>()
             .IsValidDomainAsync("google.com")
             .Returns(Task.FromResult(true));

            container.Resolve<IFileTypeInterpreter>()
             .GetFileTypeFromFileName(Arg.Any<string>())
             .Returns(FileType.Image);

            const string text = "http://google.com foo.com/img.jpg";

            Assert.IsTrue(
                await systemUnderTest.HasLinkOfTypeAsync(
                    text, LinkType.Http));
            Assert.IsTrue(
                await systemUnderTest.HasLinkOfTypeAsync(
                    text, LinkType.ImageFile));
        }

        [TestMethod]
        public void NormalLinkHasNoType()
        {
            const string text = "google.com";
            Assert.AreEqual(
                LinkType.NoType, 
                systemUnderTest.GetLinkType(text));
        }

        [TestMethod]
        public void HttpsLinkHasHttpsType()
        {
            const string text = "https://google.com";
            Assert.IsTrue(
                systemUnderTest.GetLinkType(text)
                          .HasFlag(LinkType.Https));
        }
    }
}