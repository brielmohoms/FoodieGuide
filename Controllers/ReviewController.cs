using System;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Logging;

namespace FoodieGuide.Web.Controllers
{
    public class ReviewController : SurfaceController
    {
        private readonly IContentService _contentService;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IPublishedUrlProvider _urlProvider;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(
            IUmbracoContextAccessor umbracoContextAccessor,
            IUmbracoDatabaseFactory databaseFactory,
            ServiceContext services,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IPublishedUrlProvider publishedUrlProvider,
            IContentService contentService,
            ILogger<ReviewController> logger
        ) : base(
                umbracoContextAccessor,
                databaseFactory,
                services,
                appCaches,
                profilingLogger,
                publishedUrlProvider
             )
        {
            _contentService = contentService;
            _umbracoContextAccessor = umbracoContextAccessor;
            _urlProvider = publishedUrlProvider;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitReview(
            [FromForm] int restaurantId,
            [FromForm] string name,
            [FromForm] int rating,
            [FromForm] string comment)
        {
            var review = _contentService.Create(name, restaurantId, "reviews");

            review.SetValue("reviewerName", name);
            review.SetValue("reviewRating", rating);
            review.SetValue("comment", comment);
            review.SetValue("date", DateTime.Now);

            _contentService.Save(review);

            var publishResult = _contentService.Publish(review, new[] { "*" });

            return Redirect(_urlProvider.GetUrl(restaurantId));
        }
    }
}