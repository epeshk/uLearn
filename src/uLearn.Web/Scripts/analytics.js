﻿$(document).ready(function () {
	var editMode = false;
	var submittingData = false;

	var errorOnUpdateAdditionalScore = function ($input, error) {
		error = error || 'Неверное значение';
		$input.show().tooltip({
			title: error,
			trigger: 'manual',
			placement: 'left',
		}).tooltip('show');
		$input.focus();
		$input.data('openNext', '');
	}

	var validateAdditionalScoreInput = function ($input) {
		var value = $input.val();
		if (value === "")
			return true;
		
		var minValue = parseInt($input.attr('min')),
			maxValue = parseInt($input.attr('max'));

		if (! /^\+?(0|[1-9]\d*)$/.test(value)) {
			errorOnUpdateAdditionalScore($input, 'Введите целое число от ' + minValue + ' до ' + maxValue);
			return false;
		}

		value = parseInt(value);
		if (value < minValue || value > maxValue) {
			errorOnUpdateAdditionalScore($input, 'Баллы должны быть от ' + minValue + ' до ' + maxValue);
			return false;
		}

		return true;
	}

	var openNext = function($input) {
		if ($input.data('openNext')) {
			var $openNext = $input.data('openNext');
			$input.data('openNext', '');
			if (editMode)
				$openNext.focus();
			else
				$openNext.click();
		}
	}

	var hideAdditionalScoreInput = function ($input) {
		$input.tooltip('hide').tooltip('destroy');
		var $link = $input.parent().find('.additional-score-link');
		$input.hide();
		$link.show();
	}

	var updateEditModeControls = function () {
		$('.additional-scores__edit-mode__link').toggle(!editMode);
		$('.additional-scores__edit-mode__save-button').toggle(editMode);
	}

	var saveAdditionalScore = function($input) {
		var newScore = $input.val();
		$input.tooltip('hide').tooltip('destroy');
		submittingData = true;
		$.post($input.data('url'),
			{
				score: newScore
			}, 'json')
			.success(function(data) {
				if (data.status === 'ok') {
					var $link = $input.parent().find('.additional-score-link');
					$link.text(data.score === '' ? '—' : data.score);
					hideAdditionalScoreInput($input);

					/* Close edit mode if all scores has been accepted */
					if (editMode) {
						editMode = $('.additional-score-input:visible').length > 0;
						updateEditModeControls();
					}
				} else {
					errorOnUpdateAdditionalScore($input, data.error);
				}
			})
			.error(function() {
				errorOnUpdateAdditionalScore($input);
			})
			.always(function() {
				submittingData = false;
			});
		openNext($input);
	}

	var searchUntilHasChild = function ($item, selector, nextFunction) {
		$item = nextFunction($item);
		while ($item.length) {
			if ($item.find(selector).length)
				return $item;
			$item = nextFunction($item);
		}
		return false;
	}

	var nextUntilHasChild = function($item, selector) {
		return searchUntilHasChild($item, selector, function($item) { return $item.next(); });
	}

	var prevUntilHasChild = function($item, selector) {
		return searchUntilHasChild($item, selector, function($item) { return $item.prev(); });
	}
	
	$('.additional-score-link').click(function(e) {
		e.preventDefault();

		var $self = $(this);

		if ($('.additional-score-input:visible').length) {
			var $first = $('.additional-score-input:visible').first();
			if (! validateAdditionalScoreInput($first))
				return;
		}
		
		var $input = $self.parent().find('.additional-score-input');
		$self.hide();
		$input.show().focus().select();
	});

	$('.additional-score-input').blur(function () {
		if (editMode)
			return;

		var $self = $(this);
		if (validateAdditionalScoreInput($self)) {
			$self.hide();
			saveAdditionalScore($self);
		}
	});

	$('.additional-score-input').keydown(function (e) {
		var $self = $(this);
		var $next = undefined;
		var $studentRow = $self.closest('.student');
		var $nextStudentRow;
		if (e.which === 9) // Tab or Shift+Tab
		{
			var $additionalScore = $self.closest('.additional-score');
			var $nextAdditionalScore = nextUntilHasChild($additionalScore, '.additional-score-link');
			if (e.shiftKey)
				$nextAdditionalScore = prevUntilHasChild($additionalScore, '.additional-score-link');
			if ($nextAdditionalScore) {
				$next = $nextAdditionalScore.find('.additional-score-link');
			} else {
				/* Jump to next (or previous) row if current cell is last (or first) */
				if (!e.shiftKey) {
					$nextStudentRow = nextUntilHasChild($studentRow, '.additional-score-link');
					if ($nextStudentRow)
						$next = $nextStudentRow.find('.additional-score-link').first();
				} else {
					$nextStudentRow = prevUntilHasChild($studentRow, '.additional-score-link');
					if ($nextStudentRow)
						$next = $nextStudentRow.find('.additional-score-link').last();
				}
			}
		}
		if (e.which === 13 || e.which === 38 || e.which === 40) // Enter, Up or Down
		{
			var scoringType = $self.data('scoringType');
			var selector = '.additional-score-link[data-scoring-type=' + scoringType + ']';
			$nextStudentRow = nextUntilHasChild($studentRow, selector);
			if (e.which === 38)
				$nextStudentRow = prevUntilHasChild($studentRow, selector);
			if ($nextStudentRow.hasClass('student')) {
				if (editMode) {
					$next = $nextStudentRow.find('.additional-score-input[data-scoring-type=' + scoringType + ']');
				} else {
					$next = $nextStudentRow.find(selector);
				}
			}
		}
		if ($next && $next.length) {
			e.preventDefault();
			if (editMode)
				$next.focus();
			else {
				$self.data('openNext', $next);
				$self.blur();
			}
		}
	});

	$('.additional-scores__edit-mode__link').click(function(e) {
		e.preventDefault();
		editMode = true;
		updateEditModeControls();

		$('.additional-score-link').hide();
		$('.additional-score-input').show().first().focus();
	});

	$('.additional-scores__edit-mode__save-button').click(function(e) {
		e.preventDefault();
		$('.additional-score-input:visible').each(function () {
			var $input = $(this);
			if (validateAdditionalScoreInput($input))
				saveAdditionalScore($input);
		});
	});
});