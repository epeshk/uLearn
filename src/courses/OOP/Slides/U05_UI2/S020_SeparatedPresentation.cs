﻿using uLearn;

namespace OOP.Slides.U05_UI2
{
	[Slide("Тестируемость интерфейса", "33ECEB36C0804F8D939F0D4138E06CB7")]
	public class S020_SeparatedPresentation
	{
		/*
		Автоматизировать тесты на корректность поведения пользовательского интерфейса можно (есть разные технологии), но сложно и дорого.
		Кроме того, такие тесты оказываются очень трудны в поддержке.

		Поэтому часто используют такой подход — выносят весь код, который потенциально может сломаться в отдельные классы, не зависящие от контролов и отображения.
		Классы, ответственные за UI после этого оказываются максимально простыми без существенной логики и не нуждаются в сложном тестировании.

		Эта простая идея получила распространение под разными именами: 
		[MVC](http://martinfowler.com/eaaDev/uiArchs.html#ModelViewController), 
		[Separated Presentation](http://martinfowler.com/eaaDev/SeparatedPresentation.html), 
		[Humble View](http://www.objectmentor.com/resources/articles/TheHumbleDialogBox.pdf).

		Упрощенные классы, ответственные за UI мы будем называть Представлением.

		Классы, в которые переместится вся существенная логика работы, которую стоит тестировать будем называть Логикой. 
		Иногда логику подразделяют на Модель и Контроллер.

		Модель — это классы содержащие все данные, которые нужно отобразить 
		и методы работы с этими данными, которые не предполагают наличие Представления.
		Логику выделенную в модель легко тестировать обычными методами.
		
		Контроллер — классы, не содержащие отображаемых данных, но содержащие всю существенную логику обновления Представления и 
		реакции на действия пользователя в интерфейса.

		Если при этом Контроллер будет использовать Представление через интерфейс, 
		то появится возможность подменять одну реализацию Представления на другую,
		ведь само Представление содержит минимум кода.

		В частности в тестах можно подменить Представление на тестовую заглушку, 
		и протестировать корректность работы Контроллера в паре с тестовым Представлением.

		Все это можно отобразить на следующей диаграмме:

		![separated-presentation.png](separated-presentation.png)
		*/

	}
}