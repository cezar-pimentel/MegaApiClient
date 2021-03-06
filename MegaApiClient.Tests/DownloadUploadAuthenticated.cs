﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.Interfaces;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class DownloadUploadAuthenticated : DownloadUpload
    {
        public DownloadUploadAuthenticated()
            : this(null)
        {
        }

        protected DownloadUploadAuthenticated(Options? options)
            : base(Options.LoginAuthenticated | Options.Clean | options.GetValueOrDefault())
        {
        }

        [Test]
        public void DownloadNode_ToStream_Succeeds()
        {
            const string ExpectedFile = "Data/SampleFile.jpg";
            INode node = this.Client.GetNodes().Single(x => x.Id == this.PermanentFile);

            using (Stream stream = this.Client.Download(node))
            {
                using (Stream expectedStream = new FileStream(ExpectedFile, FileMode.Open))
                {
                    this.AreStreamsEquivalent(stream, expectedStream);
                }
            }
        }

        [TestCaseSource(typeof(DownloadUploadAuthenticated), nameof(GetGetDownloadLinkInvalidParameter))]
        public void GetDownloadLink_InvalidNode_Throws(INode node, IResolveConstraint constraint)
        {
            Assert.That(
                () => this.Client.GetDownloadLink(node),
                constraint);
        }

        [Test]
        public void UploadStream_DownloadLink_Succeeds()
        {
            byte[] data = new byte[123];
            this.random.NextBytes(data);

            INode parent = this.GetNode(NodeType.Root);

            INode node;
            using (Stream stream = new MemoryStream(data))
            {
                node = this.Client.Upload(stream, "test", parent);

                Uri uri = this.Client.GetDownloadLink(node);

                stream.Position = 0;
                this.AreStreamsEquivalent(this.Client.Download(uri), stream);
            }
        }

        [TestCase("https://mega.nz/#!m9Q20Qwa!RSz1DoCSGANrpphQtkr__uACIUZsFkiPWEkldOHNO20")]
        public void DownloadLink_Succeeds(string expectedLink)
        {
            INode node = this.Client.GetNodes().Single(x => x.Id == this.PermanentFile);

            Assert.That(
                this.Client.GetDownloadLink(node),
                Is.EqualTo(new Uri(expectedLink)));
        }

        private static IEnumerable<ITestCaseData> GetGetDownloadLinkInvalidParameter()
        {
            yield return new TestCaseData(null, Throws.TypeOf<ArgumentNullException>());

            foreach (NodeType nodeType in new[] { NodeType.Inbox, NodeType.Root, NodeType.Trash })
            {
                Mock<INode> nodeMock = new Mock<INode>();
                nodeMock.SetupGet(x => x.Type).Returns(nodeType);
                yield return new TestCaseData(
                    nodeMock.Object,
                    Throws
                        .TypeOf<ArgumentException>()
                        .And.Message.EqualTo("Invalid node"));
            }

            Mock<INode> validTypeNodeMock = new Mock<INode>();
            validTypeNodeMock.SetupGet(x => x.Type).Returns(NodeType.File);
            yield return new TestCaseData(
                validTypeNodeMock.Object,
                Throws
                    .TypeOf<ArgumentException>()
                    .And.Message.EqualTo("node must implement INodeCrypto"));
        }
    }
}
