export function initPasswordPolicy(opts) {
	const {
		passwordSelector = '#Password',
		firstNameSelector = '#FirstName',
		lastNameSelector = '#LastName',
		emailSelector = '#Email',
		policyRootSelector = '#password-policy'
	} = opts || {};

	const el = document.querySelector(passwordSelector);
	const policy = document.querySelector(policyRootSelector);
	if (!el || !policy) return;

	const first = document.querySelector(firstNameSelector);
	const last = document.querySelector(lastNameSelector);
	const email = document.querySelector(emailSelector);

	const rules = {
		len: v => v.length >= 10,
		upper: v => /[A-Z]/.test(v),
		lower: v => /[a-z]/.test(v),
		digit: v => /\d/.test(v),
		symbol: v => /[^A-Za-z0-9\s]/.test(v)
	};

	const items = {};
	policy.querySelectorAll('[data-rule]').forEach(li => {
		items[li.dataset.rule] = li;
	});

	function update() {
		const v = el.value || '';
		Object.entries(rules).forEach(([k, fn]) => {
			const ok = !!fn(v);
			const li = items[k];
			if (!li) return;
			li.classList.toggle('ok', ok);
			li.classList.toggle('bad', !ok);
		});
	}

	el.addEventListener('input', update);
	[first, last, email].forEach(inp => inp && inp.addEventListener('input', update));
	update();
}

