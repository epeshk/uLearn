﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using uLearn.Web.Models;

namespace uLearn.Web.DataContexts
{
	public class VisitsRepo
	{
		private readonly ULearnDb db;
		private readonly SlideCheckingsRepo slideCheckingsRepo;

		public VisitsRepo(ULearnDb db)
		{
			this.db = db;
			slideCheckingsRepo = new SlideCheckingsRepo(db);
		}

		public async Task AddVisit(string courseId, Guid slideId, string userId)
		{
			if (IsUserVisitedSlide(courseId, slideId, userId))
				return;
			db.Visits.Add(new Visit
			{
				UserId = userId,
				CourseId = courseId,
				SlideId = slideId,
				Timestamp = DateTime.Now
			});
			await db.SaveChangesAsync();
		}

		public int GetVisitsCount(Guid slideId, string courseId)
		{
			return db.Visits.Count(x => x.CourseId == courseId && x.SlideId == slideId);
		}

		public bool IsUserVisitedSlide(string courseId, Guid slideId, string userId)
		{
			return db.Visits.Any(v => v.CourseId == courseId && v.SlideId == slideId && v.UserId == userId);
		}

		public HashSet<Guid> GetIdOfVisitedSlides(string courseId, string userId)
		{
			return new HashSet<Guid>(db.Visits.Where(v => v.UserId == userId && v.CourseId == courseId).Select(x => x.SlideId));
		}
		
		public bool HasVisitedSlides(string courseId, string userId)
		{
			return db.Visits.Any(v => v.CourseId == courseId && v.UserId == userId);
		}

		public async Task UpdateScoreForVisit(string courseId, Guid slideId, string userId)
		{
			var newScore = slideCheckingsRepo.GetManualScoreForSlide(courseId, slideId, userId) +
							slideCheckingsRepo.GetAutomaticScoreForSlide(courseId, slideId, userId);
			var isPassed = slideCheckingsRepo.IsSlidePassed(courseId, slideId, userId);
			await UpdateAttempts(slideId, userId, visit =>
			{
				visit.Score = newScore;
				visit.IsPassed = isPassed;
			});
		}

		private async Task UpdateAttempts(Guid slideId, string userId, Action<Visit> action)
		{
			var visit = db.Visits.FirstOrDefault(v => v.SlideId == slideId && v.UserId == userId);
			if (visit == null)
				return;
			action(visit);
			await db.SaveChangesAsync();
		}

		public async Task RemoveAttempts(Guid slideId, string userId)
		{
			await UpdateAttempts(slideId, userId, visit =>
			{
				visit.AttemptsCount = 0;
				visit.Score = 0;
				visit.IsPassed = false;
			});
		}

		public Dictionary<Guid, int> GetScoresForSlides(string courseId, string userId, IEnumerable<Guid> slidesIds=null)
		{
			var visits = db.Visits.Where(v => v.CourseId == courseId && v.UserId == userId);
			if (slidesIds != null)
				visits = visits.Where(v => slidesIds.Contains(v.SlideId));
			var vs = visits.Select(v => v.SlideId + " " + v.Score + " " + v.IsSkipped).ToList();
			return visits
				.GroupBy(v => v.SlideId, (s, v) => new { Key = s, Value = v.FirstOrDefault() })
				.ToDictionary(g => g.Key, g => g.Value.Score);
		}

		public List<Guid> GetSlidesWithUsersManualChecking(string courseId, string userId)
		{
			return db.Visits.Where(v => v.CourseId == courseId && v.UserId == userId)
				.Where(v => v.HasManualChecking)
				.Select(v => v.SlideId)
				.ToList();
		}

		public async Task MarkVisitsAsWithManualChecking(Guid slideId, string userId)
		{
			await UpdateAttempts(slideId, userId, visit =>
			{
				visit.HasManualChecking = true;
			});
		}

		public int GetScore(Guid slideId, string userId)
		{
			return db.Visits
				.Where(v => v.SlideId == slideId && v.UserId == userId)
				.Select(v => v.Score)
				.FirstOrDefault();
		}

		public Visit FindVisiter(string courseId, Guid slideId, string userId)
		{
			return db.Visits.FirstOrDefault(v => v.CourseId == courseId && v.SlideId == slideId && v.UserId == userId);
		}

		public async Task SkipSlide(string courseId, Guid slideId, string userId)
		{
			var visiter = FindVisiter(courseId, slideId, userId);
			if (visiter != null)
				visiter.IsSkipped = true;
			else
				db.Visits.Add(new Visit
				{
					UserId = userId,
					CourseId = courseId,
					SlideId = slideId,
					Timestamp = DateTime.Now,
					IsSkipped = true
				});
			await db.SaveChangesAsync();
		}

		public bool IsSkipped(string courseId, Guid slideId, string userId)
		{
			return db.Visits.Any(v => v.CourseId == courseId && v.SlideId == slideId && v.UserId == userId && v.IsSkipped);
		}

		public bool IsPassed(Guid slideId, string userId)
		{
			return db.Visits.Any(v => v.SlideId == slideId && v.UserId == userId && v.IsPassed);
		}

		public bool IsSkippedOrPassed(Guid slideId, string userId)
		{
			return db.Visits.Any(v => v.SlideId == slideId && v.UserId == userId && (v.IsPassed || v.IsSkipped));
		}

		public async Task AddVisits(IEnumerable<Visit> visits)
		{
			foreach (var visit in visits)
			{
				if (db.Visits.Any(x => x.UserId == visit.UserId && x.SlideId == visit.SlideId))
					continue;
				db.Visits.Add(visit);
			}
			await db.SaveChangesAsync();
		}

		public IQueryable<Visit> GetVisitsInPeriod(IEnumerable<Guid> slidesIds, DateTime periodStart, DateTime periodFinish, IEnumerable<string> usersIds=null)
		{
			var filteredVisits = db.Visits.Where(v => slidesIds.Contains(v.SlideId) && periodStart <= v.Timestamp && v.Timestamp <= periodFinish);
			if (usersIds != null)
				filteredVisits = filteredVisits.Where(v => usersIds.Contains(v.UserId));
			return filteredVisits;
		}

		public IQueryable<Visit> GetVisitsInPeriod(VisitsFilterOptions options)
		{
			var filteredVisits = db.Visits.Where(v => options.PeriodStart <= v.Timestamp && v.Timestamp <= options.PeriodFinish);
			if (options.SlidesIds != null)
				filteredVisits = filteredVisits.Where(v => options.SlidesIds.Contains(v.SlideId));
			if (options.UsersIds != null)
			{
				if (options.IsUserIdsSupplement)
					filteredVisits = filteredVisits.Where(v => ! options.UsersIds.Contains(v.UserId));
				else
					filteredVisits = filteredVisits.Where(v => options.UsersIds.Contains(v.UserId));
			}
			return filteredVisits;
		}

		public Dictionary<Guid, List<Visit>> GetVisitsInPeriodForEachSlide(VisitsFilterOptions options)
		{
			return GetVisitsInPeriod(options)
				.GroupBy(v => v.SlideId)
				.ToDictionary(g => g.Key, g => g.ToList());
		}

		public IEnumerable<string> GetUsersVisitedAllSlides(IImmutableSet<Guid> slidesIds, DateTime periodStart, DateTime periodFinish, IEnumerable<string> usersIds = null)
		{
			var slidesCount = slidesIds.Count;

			return GetVisitsInPeriod(slidesIds, periodStart, periodFinish, usersIds)
				.DistinctBy(v => Tuple.Create(v.UserId, v.SlideId))
				.GroupBy(v => v.UserId)
				.Where(g => g.Count() == slidesCount)
				.Select(g => g.Key);
		}

		public IEnumerable<string> GetUsersVisitedAllSlides(VisitsFilterOptions options)
		{
			if (options.SlidesIds == null)
				throw new ArgumentNullException(nameof(options.SlidesIds));

			var slidesCount = options.SlidesIds.Count();

			return GetVisitsInPeriod(options)
				.DistinctBy(v => Tuple.Create(v.UserId, v.SlideId))
				.GroupBy(v => v.UserId)
				.Where(g => g.Count() == slidesCount)
				.Select(g => g.Key);
		}

		public HashSet<string> GetUserCourses(string userId)
		{
			return new HashSet<string>(db.Visits.Where(v => v.UserId == userId).Select(v => v.CourseId).Distinct());
		}
	}
}