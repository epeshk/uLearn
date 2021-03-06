﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace uLearn
{
	public class Unit
	{
		public Unit(UnitSettings settings, DirectoryInfo directory)
		{
			Settings = settings;
			Directory = directory;
		}

		private UnitSettings Settings { get; set; }

		public InstructorNote InstructorNote { get; set; }

		public List<Slide> Slides { get; set; }

		public DirectoryInfo Directory { get; set; }

		public Guid Id => Settings.Id;

		public string Title => Settings.Title;

		public string Url => Settings.Url;

		public ScoringSettings Scoring => Settings.Scoring;

		public void LoadInstructorNote()
		{
			var instructorNoteFile = Directory.GetFile("InstructorNotes.md");
			if (instructorNoteFile.Exists)
			{
				InstructorNote = InstructorNote.Load(instructorNoteFile, this);
			}
		}
	}
}