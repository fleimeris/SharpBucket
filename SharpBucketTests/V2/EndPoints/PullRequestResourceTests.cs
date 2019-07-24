﻿using System;
using System.Net;
using NUnit.Framework;
using SharpBucket.V2;
using SharpBucket.V2.EndPoints;
using SharpBucket.V2.Pocos;
using Shouldly;

namespace SharpBucketTests.V2.EndPoints
{
    [TestFixture]
    internal class PullRequestResourceTests
    {
        private PullRequestResource ExistingPullRequest { get; set; }

        private PullRequestResource NotExistingPullRequest { get; set; }

        [OneTimeSetUp]
        protected void Init()
        {
            // pull request number 2 on MercurialRepository is public and declined
            // so we could expect that it will be always accessible and won't change
            // which is what we need to have reproducible tests
            ExistingPullRequest = SampleRepositories.MercurialRepository.PullRequestsResource().PullRequestResource(2);

            // there is no change that a pull request with the max value of int32 exist one day
            NotExistingPullRequest = SampleRepositories.MercurialRepository.PullRequestsResource().PullRequestResource(int.MaxValue);
        }

        [Test]
        public void GetPullRequest_ExistingPublicPullRequest_ReturnValidInfo()
        {
            var pullRequest = ExistingPullRequest.GetPullRequest();
            pullRequest.ShouldNotBeNull();
            pullRequest.id.ShouldBe(2);
            pullRequest.author?.nickname.ShouldBe("goodtune");
            pullRequest.title.ShouldBe("Selective read/write or read-only repos with hg-ssh");
            pullRequest.state.ShouldBe("DECLINED");
        }

        [Test]
        public void GetPullRequest_NotExistingPublicPullRequest_ThrowException()
        {
            var exception = Assert.Throws<BitbucketV2Exception>(() => NotExistingPullRequest.GetPullRequest());
            exception.HttpStatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Test]
        public void GetPullRequestActivity_ExistingPublicPullRequest_ReturnValidInfo()
        {
            var activities = ExistingPullRequest.GetPullRequestActivity();
            activities.ShouldNotBeNull();
            activities.Count.ShouldBe(4);
            activities[0].update.state.ShouldBe("DECLINED");
        }

        [Test]
        public void GetPullRequestActivity_NotExistingPublicPullRequest_ThrowException()
        {
            var exception = Assert.Throws<BitbucketV2Exception>(() => NotExistingPullRequest.GetPullRequestActivity());
            exception.HttpStatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Test]
        public void ListPullRequestComments_ExistingPublicPullRequest_ReturnValidInfo()
        {
            var comments = ExistingPullRequest.ListPullRequestComments();
            comments.ShouldNotBeNull();
            comments.Count.ShouldBe(2);
            comments[0].content.raw.ShouldBe("This repo is not used for development, it's just a mirror (and I am just an infrequent contributor). Please consult http://mercurial.selenic.com/wiki/ContributingChanges and send your patch to ``mercurial-devel`` ML.");
        }

        [Test]
        public void ListPullRequestComments_NotExistingPublicPullRequest_ThrowException()
        {
            var exception = Assert.Throws<BitbucketV2Exception>(() => NotExistingPullRequest.ListPullRequestComments());
            exception.HttpStatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Test]
        public void GetPullRequestComment_ExistingCommentOnAPublicPullRequest_ReturnValidInfo()
        {
            var comment = ExistingPullRequest.GetPullRequestComment(53789);
            comment.ShouldNotBeNull();
            comment.content.raw.ShouldBe("This repo is not used for development, it's just a mirror (and I am just an infrequent contributor). Please consult http://mercurial.selenic.com/wiki/ContributingChanges and send your patch to ``mercurial-devel`` ML.");
        }

        [Test]
        public void GetPullRequestComment_NotExistingCommentOnPublicPullRequest_ThrowException()
        {
            var exception = Assert.Throws<BitbucketV2Exception>(() => ExistingPullRequest.GetPullRequestComment(int.MaxValue));
            exception.HttpStatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Test]
        public void ListPullRequestCommits_ExistingPublicPullRequest_ReturnValidInfo()
        {
            var commits = ExistingPullRequest.ListPullRequestCommits();
            commits.ShouldNotBeNull();
            commits.Count.ShouldBe(2);
            commits[0].message.ShouldBe("Update the docstring");
        }

        [Test]
        public void ListPullRequestCommits_NotExistingPublicPullRequest_ThrowException()
        {
            var exception = Assert.Throws<BitbucketV2Exception>(() => NotExistingPullRequest.ListPullRequestCommits());
            exception.HttpStatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Test]
        public void GetDiffForPullRequest_ExistingPublicPullRequest_ReturnValidInfo()
        {
            var diff = ExistingPullRequest.GetDiffForPullRequest();
            diff.ShouldNotBeNull();
            // TODO: to complete once the right POCO will be returned
        }

        [Test]
        public void GetDiffForPullRequest_NotExistingPublicPullRequest_ThrowException()
        {
            var exception = Assert.Throws<BitbucketV2Exception>(() => NotExistingPullRequest.GetDiffForPullRequest());
            exception.HttpStatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Test]
        public void DeclinePullRequest_CreateAPullRequestThenDeclineIt_BranchStateShouldChangeFromOpenToDeclined()
        {
            var pullRequestsResource = SampleRepositories.TestRepository.RepositoryResource.PullRequestsResource();
            var pullRequestToDecline = new PullRequest
            {
                title = "a bad work",
                source = new Source { branch = new Branch { name = "branchToDecline" } }
            };
            var pullRequest = pullRequestsResource.PostPullRequest(pullRequestToDecline);
            pullRequest.state.ShouldBe("OPEN");

            var declinedPullRequest = pullRequestsResource.PullRequestResource(pullRequest.id.GetValueOrDefault()).DeclinePullRequest();
            declinedPullRequest.state.ShouldBe("DECLINED");
        }

        [Test]
        public void ApprovePullRequestAndRemovePullRequestApproval_CreateAPullRequestThenChangeMyApproval_ActivityShouldFollowThatChanges()
        {
            // create the pull request
            var pullRequestsResource = SampleRepositories.TestRepository.RepositoryResource.PullRequestsResource();
            var pullRequestToApprove = new PullRequest
            {
                title = "a good work to approve",
                source = new Source { branch = new Branch { name = "branchToAccept" } }
            };
            var pullRequest = pullRequestsResource.PostPullRequest(pullRequestToApprove);
            var pullRequestResource = pullRequestsResource.PullRequestResource(pullRequest.id.GetValueOrDefault());

            // approve the pull request
            var approvalResult = pullRequestResource.ApprovePullRequest();
            approvalResult.approved.ShouldBe(true);
            approvalResult.user.nickname.ShouldBe(TestHelpers.AccountName);
            approvalResult.role.ShouldBe("PARTICIPANT");

            // validate pull request activities after approval
            var activities = pullRequestResource.GetPullRequestActivity();
            activities.Count.ShouldBeGreaterThanOrEqualTo(2, "creation twice (for an unknown reason that may change) and approve");
            var approvalActivity = activities[0];
            approvalActivity.comment.ShouldBe(null);
            approvalActivity.update.ShouldBe(null);
            approvalActivity.pull_request.ShouldNotBeNull();
            approvalActivity.pull_request.title.ShouldBe("a good work to approve");
            approvalActivity.approval.ShouldNotBeNull();
            approvalActivity.approval.date.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
            approvalActivity.approval.user.nickname.ShouldBe(TestHelpers.AccountName);

            // remove approval
            pullRequestResource.RemovePullRequestApproval();

            // validate pull request activities after having remove the approval
            var activitiesAfterRemoveApproval = pullRequestResource.GetPullRequestActivity();
            activitiesAfterRemoveApproval.Count.ShouldBe(activities.Count - 1, "Approval activity is removed, and removal is not traced.");
            activitiesAfterRemoveApproval.ShouldAllBe(activity => activity.update != null, "should all be update activities");
        }
    }
}
