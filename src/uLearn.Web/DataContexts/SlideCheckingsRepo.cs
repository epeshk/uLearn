﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using uLearn.Quizes;
using uLearn.Web.Extensions;
using uLearn.Web.Models;

namespace uLearn.Web.DataContexts
{
	public class SlideCheckingsRepo
	{
		private readonly ULearnDb db;

		public SlideCheckingsRepo(ULearnDb db)
		{
			this.db = db;
		}

		public async Task AddQuizAttemptForManualChecking(string courseId, Guid slideId, string userId)
		{
			var manualChecking = new ManualQuizChecking
			{
				CourseId = courseId,
				SlideId = slideId,
				UserId = userId,
				Timestamp = DateTime.Now,
			};
			db.ManualQuizCheckings.Add(manualChecking);

			await db.SaveChangesAsync();
		}

		public async Task AddQuizAttemptWithAutomaticChecking(string courseId, Guid slideId, string userId, int automaticScore)
		{
			var automaticChecking = new AutomaticQuizChecking
			{
				CourseId = courseId,
				SlideId = slideId,
				UserId = userId,
				Timestamp = DateTime.Now,
				Score = automaticScore,
			};
			db.AutomaticQuizCheckings.Add(automaticChecking);
			
			await db.SaveChangesAsync();
		}

		public IEnumerable<ManualExerciseChecking> GetUsersPassedManualExerciseCheckings(string courseId, string userId)
		{
			return db.ManualExerciseCheckings.Where(c => c.CourseId == courseId && c.UserId == userId && c.IsChecked).DistinctBy(c => c.SlideId);
		}

		public async Task<ManualExerciseChecking> AddManualExerciseChecking(string courseId, Guid slideId, string userId, UserExerciseSubmission submission)
		{
			var manualChecking = new ManualExerciseChecking
			{
				CourseId = courseId,
				SlideId = slideId,
				UserId = userId,
				Timestamp = DateTime.Now,

				SubmissionId = submission.Id,
			};
			db.ManualExerciseCheckings.Add(manualChecking);

			try
			{
				await db.SaveChangesAsync();
			}
			catch (DbEntityValidationException e)
			{
				throw new Exception(
					string.Join("\r\n", e.EntityValidationErrors.Select(v => v.Entry.Entity.ToString())) + 
					string.Join("\r\n",
					e.EntityValidationErrors.SelectMany(v => v.ValidationErrors).Select(err => err.PropertyName + " " + err.ErrorMessage)));
			}

			return manualChecking;
		}
		
		public async Task RemoveWaitingManualExerciseCheckings(string courseId, Guid slideId, string userId)
		{
			using (var transaction = db.Database.BeginTransaction())
			{
				var checkings = GetSlideCheckingsByUser<ManualExerciseChecking>(courseId, slideId, userId, false).Where(c => !c.IsChecked && !c.IsLocked);
				foreach (var checking in checkings)
					// Use EntityState.Deleted because EF could don't know abount these checkings (they have been retrieved via AsNoTracking())
					db.Entry(checking).State = EntityState.Deleted;
				await db.SaveChangesAsync();
				transaction.Commit();
			}
		}

		private IEnumerable<T> GetSlideCheckingsByUser<T>(string courseId, Guid slideId, string userId, bool noTracking = true) where T: AbstractSlideChecking
		{
			IQueryable<T> dbRef = db.Set<T>();
			if (noTracking)
				dbRef = dbRef.AsNoTracking();
			var items = dbRef.Where(c => c.CourseId == courseId && c.SlideId == slideId && c.UserId == userId).ToList();
			return items;
		}

		public async Task RemoveAttempts(string courseId, Guid slideId, string userId, bool saveChanges=true)
		{
			db.ManualQuizCheckings.RemoveSlideAction(slideId, userId);
			db.AutomaticQuizCheckings.RemoveSlideAction(slideId, userId);
			db.ManualExerciseCheckings.RemoveSlideAction(slideId, userId);
			db.AutomaticExerciseCheckings.RemoveSlideAction(slideId, userId);
			if (saveChanges)
				await db.SaveChangesAsync();
		}

		public bool IsSlidePassed(string courseId, Guid slideId, string userId)
		{
			return GetSlideCheckingsByUser<ManualQuizChecking>(courseId, slideId, userId).Any() ||
				GetSlideCheckingsByUser<AutomaticQuizChecking>(courseId, slideId, userId).Any() ||
				GetSlideCheckingsByUser<ManualExerciseChecking>(courseId, slideId, userId).Any() ||
				GetSlideCheckingsByUser<AutomaticExerciseChecking>(courseId, slideId, userId).Any(c => c.Score > 0);
		}
		
		private int GetUserScoreForSlide<T>(string courseId, Guid slideId, string userId) where T : AbstractSlideChecking
		{
			return GetSlideCheckingsByUser<T>(courseId, slideId, userId).Select(c => c.Score).DefaultIfEmpty(0).Max();
		}

		public int GetManualScoreForSlide(string courseId, Guid slideId, string userId)
		{
			var quizScore = GetUserScoreForSlide<ManualQuizChecking>(courseId, slideId, userId);
			var exerciseScore = GetUserScoreForSlide<ManualExerciseChecking>(courseId, slideId, userId);

			return Math.Max(quizScore, exerciseScore);
		}

		public int GetAutomaticScoreForSlide(string courseId, Guid slideId, string userId)
		{
			var quizScore = GetUserScoreForSlide<AutomaticQuizChecking>(courseId, slideId, userId);
			var exerciseScore = GetUserScoreForSlide<AutomaticExerciseChecking>(courseId, slideId, userId);

			return Math.Max(quizScore, exerciseScore);
		}

		public IEnumerable<T> GetManualCheckingQueue<T>(ManualCheckingQueueFilterOptions options) where T : AbstractManualSlideChecking
		{
			var query = db.Set<T>().Where(c => c.CourseId == options.CourseId);
			query = options.OnlyChecked ? query.Where(c => c.IsChecked) : query.Where(c => !c.IsChecked);
			if (options.SlidesIds != null)
				query = query.Where(c => options.SlidesIds.Contains(c.SlideId));
			if (options.UsersIds != null)
			{
				if (options.IsUserIdsSupplement)
					query = query.Where(c => ! options.UsersIds.Contains(c.UserId));
				else
					query = query.Where(c => options.UsersIds.Contains(c.UserId));
			}
			query = query.OrderByDescending(c => c.Timestamp);
			if (options.Count > 0)
				query = query.Take(options.Count);
			return query;
		}
		
		public T FindManualCheckingById<T>(int id) where T : AbstractManualSlideChecking
		{
			return db.Set<T>().Find(id);
		}
		
		public bool IsProhibitedToSendExerciseToManualChecking(string courseId, Guid slideId, string userId)
		{
			return GetSlideCheckingsByUser<ManualExerciseChecking>(courseId, slideId, userId).Any(c => c.ProhibitFurtherManualCheckings);
		}

		public async Task LockManualChecking<T>(T checkingItem, string lockedById) where T : AbstractManualSlideChecking
		{
			checkingItem.LockedById = lockedById;
			checkingItem.LockedUntil = DateTime.Now.Add(TimeSpan.FromMinutes(30));
			await db.SaveChangesAsync();
		}

		public async Task MarkManualCheckingAsChecked<T>(T queueItem, int score) where T : AbstractManualSlideChecking
		{
			queueItem.LockedBy = null;
			queueItem.LockedUntil = null;
			queueItem.IsChecked = true;
			queueItem.Score = score;
			await db.SaveChangesAsync();
		}

		public async Task ProhibitFurtherExerciseManualChecking(ManualExerciseChecking checking)
		{
			checking.ProhibitFurtherManualCheckings = true;
			await db.SaveChangesAsync();
		}

		public async Task<ExerciseCodeReview> AddExerciseCodeReview(ManualExerciseChecking checking, string userId, int startLine, int startPosition, int finishLine, int finishPosition, string comment)
		{
			var review = db.ExerciseCodeReviews.Add(new ExerciseCodeReview
			{
				AuthorId = userId,
				Comment = comment,
				ExerciseCheckingId = checking.Id,
				StartLine = startLine,
				StartPosition = startPosition,
				FinishLine = finishLine,
				FinishPosition = finishPosition,
			});

			try
			{
				await db.SaveChangesAsync();
			}
			catch (DbEntityValidationException e)
			{
				throw new Exception(
					string.Join("\r\n",
						e.EntityValidationErrors.SelectMany(v => v.ValidationErrors).Select(err => err.PropertyName + " " + err.ErrorMessage)));
			}

			/* Extract review from database to fill review.Author by EF's DynamicProxy */
			return db.ExerciseCodeReviews.AsNoTracking().FirstOrDefault(r => r.Id == review.Id);
		}

		public ExerciseCodeReview FindExerciseCodeReviewById(int reviewId)
		{
			return db.ExerciseCodeReviews.Find(reviewId);
		}

		public async Task DeleteExerciseCodeReview(ExerciseCodeReview review)
		{
			review.IsDeleted = true;
			await db.SaveChangesAsync();
		}

		public async Task UpdateExerciseCodeReview(ExerciseCodeReview review, string newComment)
		{
			review.Comment = newComment;
			await db.SaveChangesAsync();
		}
		
		public Dictionary<int, List<ExerciseCodeReview>> GetExerciseCodeReviewForCheckings(IEnumerable<int> checkingsIds)
		{
			return db.ExerciseCodeReviews
				.Where(r => checkingsIds.Contains(r.ExerciseCheckingId) && ! r.IsDeleted)
				.GroupBy(r => r.ExerciseCheckingId)
				.ToDictionary(g => g.Key, g => g.ToList());
		}

		public List<string> GetTopUserReviewComments(string courseId, Guid slideId, string userId, int count)
		{
			return db.ExerciseCodeReviews.Include(r => r.ExerciseChecking)
				.Where(r => r.ExerciseChecking.CourseId == courseId &&
							r.ExerciseChecking.SlideId == slideId &&
							r.AuthorId == userId &&
							! r.HiddenFromTopComments &&
							! r.IsDeleted)
				.GroupBy(r => r.Comment)
				.OrderByDescending(g => g.Count())
				.ThenByDescending(g => g.Max(r => r.ExerciseChecking.Timestamp))
				.Take(count)
				.Select(g => g.Key)
				.ToList();
		}

		public async Task HideFromTopCodeReviewComments(string courseId, Guid slideId, string userId, string comment)
		{
			var reviews = db.ExerciseCodeReviews.Include(r => r.ExerciseChecking)
				.Where(r => r.ExerciseChecking.CourseId == courseId &&
							r.ExerciseChecking.SlideId == slideId &&
							r.AuthorId == userId &&
							r.Comment == comment && 
							!r.IsDeleted);

			foreach (var review in reviews)
				review.HiddenFromTopComments = true;
			await db.SaveChangesAsync();
		}

		public List<ExerciseCodeReview> GetAllReviewComments(string courseId, Guid slideId)
		{
			return db.ExerciseCodeReviews.Where(
				r => r.ExerciseChecking.CourseId == courseId &&
					r.ExerciseChecking.SlideId == slideId &&
					!r.IsDeleted
				).ToList();
		}
	}
}