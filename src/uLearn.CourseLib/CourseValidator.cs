using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using RunCsJob;
using RunCsJob.Api;
using uLearn.Model.Blocks;

namespace uLearn
{
	public class CourseValidator
	{
		private readonly List<Slide> slides;
		private readonly SandboxRunnerSettings settings;

		public event Action<string> InfoMessage;
		public event Action<string> Error;

		public CourseValidator(List<Slide> slides, SandboxRunnerSettings settings)
		{
			this.slides = slides;
			this.settings = settings;
		}

		public void ValidateExercises()
		{
			foreach (var slide in slides.OfType<ExerciseSlide>())
			{
				LogSlideProcessing("Validate exercise", slide);
				EthalonSolutionsForExercises(slide);
				if (slide.Exercise is ProjectExerciseBlock)
					StudentsZipIsBuildingOk(slide, (ProjectExerciseBlock)slide.Exercise);
			}
		}

		private void StudentsZipIsBuildingOk(Slide slide, ProjectExerciseBlock ex)
		{
			var tempDir = new DirectoryInfo("./temp");
			try
			{
				Utils.UnpackZip(ex.StudentsZip.Content(), "./temp");
				var res = MsBuildRunner.BuildProject(settings.MsBuildSettings, tempDir.GetFile(ex.CsprojFileName).FullName, tempDir);
				if (!res.Success)
					ReportSlideError(slide, ex.CsprojFileName + " not building! " + res);
			}
			finally
			{
				tempDir.Delete(true);
			}
		}

		private void LogSlideProcessing(string prefix, Slide slide)
		{
			InfoMessage?.Invoke(prefix + " " + slide.Info.Unit.Title + " - " + slide.Title);
		}

		public void ValidateVideos()
		{
			var videos = GetVideos().ToLookup(d => d.Item2, d => d.Item1);
			foreach (var g in videos.Where(g => g.Count() > 1))
				ReportError("Duplicate videos on slides " + string.Join(", ", g));
			foreach (var g in videos)
			{
				var slide = g.First();
				LogSlideProcessing("Validate video", slide);
				var url = "https://www.youtube.com/oembed?format=json&url=http://www.youtube.com/watch?v=" + g.Key;
				try
				{
					new WebClient().DownloadData(url);
				}
				catch (Exception e)
				{
					ReportError("Slide " + slide + " contains not accessible video. " + e.Message);
				}
			}
		}

		private void ReportSlideError(Slide slide, string error)
		{
			ReportError(slide.Info.Unit.Title + ": " + slide.Title + ". " + error);
		}

		private void ReportError(string message)
		{
			Error?.Invoke(message);
		}

		public IEnumerable<Tuple<Slide, string>> GetVideos()
		{
			return slides
				.SelectMany(slide =>
					slide.Blocks.OfType<YoutubeBlock>()
						.Select(b => Tuple.Create(slide, b.VideoId)));
		}

		private void InitialCodeIsNotSolutionForProjExercise(ExerciseSlide slide)
		{
			var exercise = (ProjectExerciseBlock)slide.Exercise;
			var directoryName = Path.Combine(exercise.SlideFolderPath.FullName, exercise.ExerciseDir);
			var excluded = (exercise.PathsToExcludeForChecker ?? new string[0]).Concat(new[] { "bin/*", "obj/*" }).ToList();
			var exerciseDir = new DirectoryInfo(directoryName);
			var bytes = exerciseDir.ToZip(excluded, new[]
			{
				new FileContent
				{
					Path = exercise.CsprojFileName,
					Data = ProjModifier.ModifyCsproj(exerciseDir.GetFile(exercise.CsprojFileName),
						proj => ProjModifier.PrepareForChecking(proj, exercise, excluded))
				}
			});
			var result = SandboxRunner.Run(new ProjRunnerSubmission
			{
				Id = slide.Id.ToString(),
				ZipFileData = bytes,
				ProjectFileName = exercise.CsprojFileName,
				Input = "",
				NeedRun = true
			});
			var isOk = result.Verdict.IsOneOf(Verdict.Ok, Verdict.MemoryLimit, Verdict.TimeLimit);
			if (!isOk)
				ReportSlideError(slide, "Exercise initial code verdict is not OK. RunResult = " + result);
			else if (result.Verdict == Verdict.Ok && result.Output == "")
				ReportSlideError(slide, "Exercise initial code (available to students) is solution!");
		}

		private void EthalonSolutionForSingleFileExercises(ExerciseSlide slide)
		{
			var exercise = (SingleFileExerciseBlock)slide.Exercise;
			var solution = exercise.BuildSolution(exercise.EthalonSolution);
			if (solution.HasErrors)
			{
				FailOnError(slide, solution, exercise.EthalonSolution);
				return;
			}
			if (solution.HasStyleIssues)
			{
				Console.WriteLine("Style issue: " + solution.StyleMessage);
			}

			var result = SandboxRunner.Run(exercise.CreateSubmition(
				slide.Id.ToString(),
				exercise.EthalonSolution), settings);

			var output = result.GetOutput().NormalizeEoln();

			var isRightAnswer = output.NormalizeEoln().Equals(slide.Exercise.ExpectedOutput.NormalizeEoln());
			if (!isRightAnswer)
			{
				ReportSlideError(slide,
					"ActualOutput: " + output.NormalizeEoln() + "\n" +
					"ExpectedOutput: " + slide.Exercise.ExpectedOutput.NormalizeEoln() + "\n" +
					"CompilationError: " + result.CompilationOutput + "\n" +
					"SourceCode: " + solution.SourceCode + "\n\n");
			}
		}

		private void EthalonSolutionsForExercises(ExerciseSlide slide)
		{
			if (slide.Exercise is ProjectExerciseBlock)
				InitialCodeIsNotSolutionForProjExercise(slide);
			else
				EthalonSolutionForSingleFileExercises(slide);
		}

		private void FailOnError(ExerciseSlide slide, SolutionBuildResult solution, string ethalonSolution)
		{
			ReportSlideError(slide, $@"Template solution: {ethalonSolution}
source code: {solution.SourceCode}
error: {solution.ErrorMessage}");
		}
	}
}