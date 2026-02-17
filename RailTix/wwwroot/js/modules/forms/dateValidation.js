(function () {
	"use strict";

	function pad(value) {
		return String(value).padStart(2, "0");
	}

	function formatForInput(date, type) {
		var year = date.getFullYear();
		var month = pad(date.getMonth() + 1);
		var day = pad(date.getDate());

		if (type === "date") {
			return year + "-" + month + "-" + day;
		}

		var hours = pad(date.getHours());
		var minutes = pad(date.getMinutes());
		return year + "-" + month + "-" + day + "T" + hours + ":" + minutes;
	}

	function parseInputValue(value, type) {
		if (!value) {
			return null;
		}

		if (type === "date") {
			var dateMatch = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
			if (!dateMatch) {
				return null;
			}

			var y = Number(dateMatch[1]);
			var m = Number(dateMatch[2]);
			var d = Number(dateMatch[3]);
			var parsedDate = new Date(y, m - 1, d, 0, 0, 0, 0);
			if (
				parsedDate.getFullYear() !== y ||
				parsedDate.getMonth() !== m - 1 ||
				parsedDate.getDate() !== d
			) {
				return null;
			}

			return parsedDate;
		}

		var dateTimeMatch = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/.exec(value);
		if (!dateTimeMatch) {
			return null;
		}

		var year = Number(dateTimeMatch[1]);
		var month = Number(dateTimeMatch[2]);
		var day = Number(dateTimeMatch[3]);
		var hour = Number(dateTimeMatch[4]);
		var minute = Number(dateTimeMatch[5]);
		var parsedDateTime = new Date(year, month - 1, day, hour, minute, 0, 0);
		if (
			parsedDateTime.getFullYear() !== year ||
			parsedDateTime.getMonth() !== month - 1 ||
			parsedDateTime.getDate() !== day ||
			parsedDateTime.getHours() !== hour ||
			parsedDateTime.getMinutes() !== minute
		) {
			return null;
		}

		return parsedDateTime;
	}

	function currentDateForType(type) {
		var now = new Date();
		now.setSeconds(0, 0);
		if (type === "date") {
			now.setHours(0, 0, 0, 0);
		}

		return now;
	}

	function findValidationMessageElement(field) {
		if (!field.form || !field.name) {
			return null;
		}

		var spans = field.form.querySelectorAll("[data-valmsg-for]");
		for (var i = 0; i < spans.length; i += 1) {
			if (spans[i].getAttribute("data-valmsg-for") === field.name) {
				return spans[i];
			}
		}

		return null;
	}

	function setFieldMessage(field, message) {
		field.setCustomValidity(message || "");
		var validationMessage = findValidationMessageElement(field);
		if (!validationMessage) {
			return;
		}

		validationMessage.textContent = message || "";
		if (message) {
			validationMessage.classList.add("field-validation-error");
		} else {
			validationMessage.classList.remove("field-validation-error");
		}
	}

	function resolveLinkedField(field, selector) {
		if (!selector || !field.form) {
			return null;
		}

		var target = field.form.querySelector(selector);
		if (target) {
			return target;
		}

		return document.querySelector(selector);
	}

	function applyBounds(field) {
		if (!(field instanceof HTMLInputElement)) {
			return;
		}

		if (field.type !== "date" && field.type !== "datetime-local") {
			return;
		}

		field.removeAttribute("min");
		field.removeAttribute("max");

		if (field.dataset.dateNotPast === "true") {
			var minValue = formatForInput(currentDateForType(field.type), field.type);
			field.min = minValue;
		}

		var maxYears = parseInt(field.dataset.dateMaxYears || "", 10);
		if (!Number.isNaN(maxYears) && maxYears > 0) {
			var maxDate = currentDateForType(field.type);
			maxDate.setFullYear(maxDate.getFullYear() + maxYears);
			field.max = formatForInput(maxDate, field.type);
		}

		var afterField = resolveLinkedField(field, field.dataset.dateAfter);
		if (afterField && afterField.value) {
			if (!field.min || afterField.value > field.min) {
				field.min = afterField.value;
			}
		}
	}

	function validateField(field) {
		if (!(field instanceof HTMLInputElement)) {
			return true;
		}

		if (field.type !== "date" && field.type !== "datetime-local") {
			return true;
		}

		if (!field.value) {
			setFieldMessage(field, "");
			return true;
		}

		var parsed = parseInputValue(field.value, field.type);
		if (!parsed) {
			setFieldMessage(field, "Enter a valid date and time.");
			return false;
		}

		if (field.dataset.dateNotPast === "true") {
			var now = currentDateForType(field.type);
			if (parsed < now) {
				setFieldMessage(
					field,
					field.dataset.dateNotPastMessage || "Please choose a date/time in the future."
				);
				return false;
			}
		}

		var afterField = resolveLinkedField(field, field.dataset.dateAfter);
		if (afterField && afterField.value) {
			var parsedAfter = parseInputValue(afterField.value, afterField.type || field.type);
			if (parsedAfter && parsed <= parsedAfter) {
				setFieldMessage(
					field,
					field.dataset.dateAfterMessage || "This date/time must be after the linked field."
				);
				return false;
			}
		}

		setFieldMessage(field, "");
		return true;
	}

	function initFormDateValidation(form) {
		var dateInputs = Array.prototype.slice.call(
			form.querySelectorAll('input[type="date"], input[type="datetime-local"]')
		);
		if (!dateInputs.length) {
			return;
		}

		function refreshFieldAndDependents(sourceField) {
			applyBounds(sourceField);
			validateField(sourceField);

			dateInputs.forEach(function (candidate) {
				if (candidate === sourceField || !candidate.dataset.dateAfter) {
					return;
				}

				var linked = resolveLinkedField(candidate, candidate.dataset.dateAfter);
				if (linked === sourceField) {
					applyBounds(candidate);
					validateField(candidate);
				}
			});
		}

		dateInputs.forEach(function (input) {
			applyBounds(input);

			input.addEventListener("input", function () {
				refreshFieldAndDependents(input);
			});

			input.addEventListener("change", function () {
				refreshFieldAndDependents(input);
			});
		});

		form.addEventListener("submit", function (event) {
			var isValid = true;
			for (var i = 0; i < dateInputs.length; i += 1) {
				applyBounds(dateInputs[i]);
				if (!validateField(dateInputs[i])) {
					isValid = false;
				}
			}

			if (isValid) {
				return;
			}

			event.preventDefault();
			var firstInvalid = dateInputs.find(function (input) {
				return !input.checkValidity();
			});

			if (!firstInvalid) {
				return;
			}

			firstInvalid.focus();
			if (typeof firstInvalid.reportValidity === "function") {
				firstInvalid.reportValidity();
			}
		});
	}

	function init() {
		var forms = document.querySelectorAll("form");
		forms.forEach(function (form) {
			initFormDateValidation(form);
		});
	}

	if (document.readyState === "loading") {
		document.addEventListener("DOMContentLoaded", init);
	} else {
		init();
	}
})();

