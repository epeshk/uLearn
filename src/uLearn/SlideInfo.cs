﻿namespace uLearn
{
	public class SlideInfo
	{
		public int Index { get; private set; }
		public string UnitName { get; private set; }

		public SlideInfo(string unitName, int index)
		{
			Index = index;
			UnitName = unitName;
		}
	}
}
