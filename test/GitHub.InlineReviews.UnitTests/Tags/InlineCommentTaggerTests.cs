using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.InlineReviews.Services;
using GitHub.InlineReviews.Tags;
using GitHub.Models;
using GitHub.Services;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using NSubstitute;
using Xunit;

namespace GitHub.InlineReviews.UnitTests.Tags
{
    public class InlineCommentTaggerTests
    {
        public class WithTextBufferInfo
        {
            [Fact]
            public void FirstPassShouldReturnEmptyTags()
            {
                var target = new InlineCommentTagger(
                    Substitute.For<IGitService>(),
                    Substitute.For<IGitClient>(),
                    Substitute.For<IDiffService>(),
                    Substitute.For<ITextView>(),
                    Substitute.For<ITextBuffer>(),
                    CreateSessionManager(leftHandSide: false));

                var result = target.GetTags(CreateSpan(10));

                Assert.Empty(result);
            }

            [Fact]
            public void ShouldReturnShowCommentTagForRhs()
            {
                var target = new InlineCommentTagger(
                    Substitute.For<IGitService>(),
                    Substitute.For<IGitClient>(),
                    Substitute.For<IDiffService>(),
                    Substitute.For<ITextView>(),
                    Substitute.For<ITextBuffer>(),
                    CreateSessionManager(leftHandSide: false));

                // Line 10 has an existing RHS comment.
                var span = CreateSpan(10);
                var firstPass = target.GetTags(span);
                var result = target.GetTags(span).ToList();

                Assert.Equal(1, result.Count);
                Assert.IsType<ShowInlineCommentTag>(result[0].Tag);
            }

            [Fact]
            public void ShouldReturnAddNewCommentTagForAddedLineOnRhs()
            {
                var target = new InlineCommentTagger(
                    Substitute.For<IGitService>(),
                    Substitute.For<IGitClient>(),
                    Substitute.For<IDiffService>(),
                    Substitute.For<ITextView>(),
                    Substitute.For<ITextBuffer>(),
                    CreateSessionManager(leftHandSide: false));

                // Line 11 has an add diff entry.
                var span = CreateSpan(11);
                var firstPass = target.GetTags(span);
                var result = target.GetTags(span).ToList();

                Assert.Equal(1, result.Count);
                Assert.IsType<AddInlineCommentTag>(result[0].Tag);
            }

            [Fact]
            public void ShouldNotReturnAddNewCommentTagForDeletedLineOnRhs()
            {
                var target = new InlineCommentTagger(
                    Substitute.For<IGitService>(),
                    Substitute.For<IGitClient>(),
                    Substitute.For<IDiffService>(),
                    Substitute.For<ITextView>(),
                    Substitute.For<ITextBuffer>(),
                    CreateSessionManager(leftHandSide: false));

                // Line 13 has an delete diff entry.
                var span = CreateSpan(13);
                var firstPass = target.GetTags(span);
                var result = target.GetTags(span).ToList();

                Assert.Empty(result);
            }

            [Fact]
            public void ShouldReturnShowCommentTagForLhs()
            {
                var target = new InlineCommentTagger(
                    Substitute.For<IGitService>(),
                    Substitute.For<IGitClient>(),
                    Substitute.For<IDiffService>(),
                    Substitute.For<ITextView>(),
                    Substitute.For<ITextBuffer>(),
                    CreateSessionManager(leftHandSide: true));

                // Line 12 has an existing LHS comment.
                var span = CreateSpan(12);
                var firstPass = target.GetTags(span);
                var result = target.GetTags(span).ToList();

                Assert.Equal(1, result.Count);
                Assert.IsType<ShowInlineCommentTag>(result[0].Tag);
            }

            [Fact]
            public void ShouldReturnAddCommentTagForLhs()
            {
                var target = new InlineCommentTagger(
                    Substitute.For<IGitService>(),
                    Substitute.For<IGitClient>(),
                    Substitute.For<IDiffService>(),
                    Substitute.For<ITextView>(),
                    Substitute.For<ITextBuffer>(),
                    CreateSessionManager(leftHandSide: true));

                // Line 13 has an delete diff entry.
                var span = CreateSpan(13);
                var firstPass = target.GetTags(span);
                var result = target.GetTags(span).ToList();

                Assert.Equal(1, result.Count);
                Assert.IsType<AddInlineCommentTag>(result[0].Tag);
            }

            static IPullRequestSessionManager CreateSessionManager(bool leftHandSide)
            {
                var diffChunk = new DiffChunk
                {
                    Lines =
                    {
                        // Line numbers here are 1-based. There is an add diff entry on line 11
                        // and a delete entry on line 13.
                        new DiffLine { Type = DiffChangeType.Add, NewLineNumber = 11 + 1 },
                        new DiffLine { Type = DiffChangeType.Delete, OldLineNumber = 13 + 1 },
                    }
                };
                var diff = new List<DiffChunk> { diffChunk };

                var rhsThread = Substitute.For<IInlineCommentThreadModel>();
                rhsThread.DiffLineType.Returns(DiffChangeType.Add);
                rhsThread.LineNumber.Returns(10);

                var lhsThread = Substitute.For<IInlineCommentThreadModel>();
                lhsThread.DiffLineType.Returns(DiffChangeType.Delete);
                lhsThread.LineNumber.Returns(12);

                // We have a comment to display on the right-hand-side of the diff view on line
                // 11 and a comment to display on line 13 on the left-hand-side.
                var threads = new List<IInlineCommentThreadModel> { rhsThread, lhsThread };

                var file = Substitute.For<IPullRequestSessionFile>();
                file.Diff.Returns(diff);
                file.InlineCommentThreads.Returns(threads);

                var session = Substitute.For<IPullRequestSession>();
                session.GetFile("file.cs").Returns(file);

                var info = new PullRequestTextBufferInfo(session, "file.cs", leftHandSide);
                var result = Substitute.For<IPullRequestSessionManager>();
                result.GetTextBufferInfo(null).ReturnsForAnyArgs(info);
                return result;
            }

            static NormalizedSnapshotSpanCollection CreateSpan(int lineNumber)
            {
                var snapshot = Substitute.For<ITextSnapshot>();
                snapshot.Length.Returns(200);

                var line = Substitute.For<ITextSnapshotLine>();
                var start = new SnapshotPoint(snapshot, lineNumber);
                var end = new SnapshotPoint(snapshot, lineNumber);
                line.LineNumber.Returns(lineNumber);
                line.Start.Returns(start);
                line.End.Returns(end);

                snapshot.GetLineFromPosition(0).ReturnsForAnyArgs(line);
                snapshot.GetLineFromLineNumber(lineNumber).Returns(line);

                var span = new Span(0, 10);
                return new NormalizedSnapshotSpanCollection(snapshot, span);
            }
        }
    }
}
