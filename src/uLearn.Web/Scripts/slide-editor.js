﻿$(document).ready(function () {
	initCodeEditor();

	$('.exercise__add-review').each(function () {
		var $self = $(this);
		var addReviewUrl = $self.data('url');

		$self.find('.exercise__add-review__comment').on('input', function () {
			if ($(this).val() === '')
				$self.find('.exercise__add-review__button').attr('disabled', 'disabled');
			else
				$self.find('.exercise__add-review__button').removeAttr('disabled');
		});

		$self.find('.exercise__add-review__button').click(function () {
			var comment = $self.find('.exercise__add-review__comment').val();
			var params = {
				StartLine: editorLastRange.anchor.line,
				StartPosition: editorLastRange.anchor.ch,
				FinishLine: editorLastRange.head.line,
				FinishPosition: editorLastRange.head.ch,
				Comment: comment,
			}
			$.post(addReviewUrl, params, function (renderedReview) {
				addExerciseCodeReview(renderedReview);
				$self.find('.exercise__add-review__comment').val('');
				$self.hide();
			});
		});
	});
});

var editorLastRange, currentReviewTextMarker, reviewsTextMarkers, exerciseCodeDoc, $exerciseCodeBlock;

function initCodeEditor($parent) {
	if (!$parent)
		$parent = $(document);
	editorLastRange = null;
	currentReviewTextMarker = null;
	reviewsTextMarkers = {};
	exerciseCodeDoc = null;

	if ($('.code-review').length > 0)
		$exerciseCodeBlock = $('.code-review')[0];
	else
		$exerciseCodeBlock = $('.code-reviewed')[0];

	CodeMirror.commands.autocomplete = function (cm) {
		cm.showHint({ hint: CodeMirror.hint.csharp });
	}

	codeMirrorClass($parent.find('.code-exercise'), true, false, false);
	codeMirrorClass($parent.find('.code-sample'), false, false, false);
	codeMirrorClass($parent.find('.code-guest'), false, true, false);
	codeMirrorClass($parent.find('.code-review'), false, false, true);
	codeMirrorClass($parent.find('.code-reviewed'), false, false, false);

	if ($exerciseCodeBlock) {
		var exerciseCodeEditor = $exerciseCodeBlock.codeMirrorEditor;
		exerciseCodeDoc = exerciseCodeEditor.getDoc();
	}

	function getMode(lang) {
		// see http://codemirror.net/mode/

		var langIds = {
			"cs": "x-csharp",
			"py": "x-python"
		};

		lang = lang || "cs";
		return "text/" + langIds[lang];
	}

	function unselectAllReviews() {
		$('.exercise__review.selected').removeClass('selected');
		if (currentReviewTextMarker) {
			currentReviewTextMarker.clear();
			currentReviewTextMarker = null;
		}
	}

	function selectReview($review) {
		var startLine = $review.data('start-line');
		var startPosition = $review.data('start-position');
		var finishLine = $review.data('finish-line');
		var finishPosition = $review.data('finish-position');

		unselectAllReviews();

		$review.addClass('selected');
		currentReviewTextMarker = exerciseCodeDoc.markText({ line: startLine, ch: startPosition }, { line: finishLine, ch: finishPosition }, {
			className: 'exercise__code__reviewed-fragment__selected'
		});
	}

	function codeMirrorClass($objects, editable, guest, review) {
		$objects.each(function () {
			var element = this;
			var $el = $(element);
			var langId = $el.data("lang");
			var editor = CodeMirror.fromTextArea(element,
			{
				mode: getMode(langId),
				lineNumbers: true,
				theme: (editable || guest) ? "cobalt" : "default",
				indentWithTabs: true,
				tabSize: 4,
				indentUnit: 4,
				extraKeys: {
					"Ctrl-Space": "autocomplete",
					".": function(cm) {
						setTimeout(function() { cm.execCommand("autocomplete"); }, 100);
						return CodeMirror.Pass;
					}
				},
				readOnly: !editable,
				//autoCloseBrackets: true, // workaround: autoCloseBracket breakes indentation after for|while|...
				styleActiveLine: editable,
				matchBrackets: true,
			});

			if (review) {
				var $addReviewPopup = $('.exercise__add-review').first();
				var $addReviewPopupInput = $addReviewPopup.find('.exercise__add-review__comment');
				
				/* Hack: focus review input just on blur */
				$addReviewPopupInput.blur(function () {
					if ($addReviewPopupInput.is(':visible')) {
						$addReviewPopupInput.focus();
					}
				});
				editor.on("beforeSelectionChange",
					function (cm, params) {
						unselectAllReviews();

						if (params.ranges < 1)
							return;

						var range = params.ranges[0];
						editorLastRange = range;
						var maxLine = Math.max(range.anchor.line, range.head.line);
						var coords = cm.cursorCoords({ line: maxLine + 1, ch: 1 }, 'page');

						if (range.anchor == range.head) {
							$addReviewPopup.hide();
							return;
						}
						$addReviewPopup.show();
						$addReviewPopup.offset({ top: coords.top, left: coords.left });

						$addReviewPopupInput.trigger('input');

						$addReviewPopupInput.focus();
					});
			}

			editor.on('cursorActivity',
				function(cm) {
					var cursor = cm.getDoc().getCursor();
					var $foundReview = false;
					$('.exercise__reviews .exercise__review')
						.each(function() {
							if ($foundReview !== false)
								return;
							var $review = $(this);
							var startLine = $review.data('start-line');
							var startPosition = $review.data('start-position');
							var finishLine = $review.data('finish-line');
							var finishPosition = $review.data('finish-position');
							if (startLine > cursor.line || (startLine == cursor.line && startPosition > cursor.ch))
								return;
							if (finishLine < cursor.line || (finishLine == cursor.line && finishPosition < cursor.ch))
								return;
							$foundReview = $review;
						});
					if ($foundReview !== false)
						selectReview($foundReview);
				});

			element.codeMirrorEditor = editor;
			if (editable)
				editor.focus();
			if (guest)
				editor.on("mousedown", function(cm) { loginForContinue(); });
		});
	}

	$('.exercise__reviews').on('click', '.exercise__review', function () {
		if (!$exerciseCodeBlock)
			return;

		var $review = $(this).closest('.exercise__review');
		selectReview($review);
	});

	$('.exercise__reviews').on('click', '.exercise__delete-review', function () {
		var $self = $(this);
		var $review = $self.closest('.exercise__review');
		var token = $('input[name="__RequestVerificationToken"]').val();
		var reviewId = $self.data('id');
		var url = $self.data('url');
		$.post(url, {
			__RequestVerificationToken: token
		}, function (data) {
			if (data.status !== 'ok') {
				console.log(data);
				alert('Can\'t delete comment: error');
			} else {
				$review.slideUp('fast', function () { $(this).remove(); });
				reviewsTextMarkers[reviewId].clear();
				unselectAllReviews();
			}
		}, 'json');
	});

	$('.exercise__reviews .exercise__review').each(function () {
		var $review = $(this);
		createMarkTextForReview($review);
	});
}

function createMarkTextForReview($review) {
	var reviewId = $review.data('id');
	var startLine = $review.data('start-line');
	var startPosition = $review.data('start-position');
	var finishLine = $review.data('finish-line');
	var finishPosition = $review.data('finish-position');

	reviewsTextMarkers[reviewId] = exerciseCodeDoc.markText({ line: startLine, ch: startPosition }, { line: finishLine, ch: finishPosition }, {
		className: 'exercise__code__reviewed-fragment'
	});
}

function addExerciseCodeReview(renderedReview) {
	var $reviews = $('.exercise__reviews');
	var $newReview = $(renderedReview);
	createMarkTextForReview($newReview);
	$reviews.append($newReview);
}

var refreshPreviousDraftLastId = undefined;
function refreshPreviousDraft(id) {
	if (id == undefined)
		id = refreshPreviousDraftLastId;
	refreshPreviousDraftLastId = id;

	window.onbeforeunload = function () {
		saveExerciseCodeDraft(id);
	}
	if (localStorage[id] != undefined && $('.code-exercise').length > 0) {
		$('.code-exercise')[0].codeMirrorEditor.setValue(localStorage[id]);
	}
}

function saveExerciseCodeDraft(id) {
	if (id == undefined)
		id = refreshPreviousDraftLastId;

	if ($('.code-exercise').length > 0)
		localStorage[id] = $('.code-exercise')[0].codeMirrorEditor.getValue();
}