function getCookie(name) {
	let matches = document.cookie.match(new RegExp("(?:^|; )" + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + "=([^;]*)"));
	return matches ? decodeURIComponent(matches[1]) : null;
}

function setCookie(name, value, options = {}) {

	options = {
		path: '/',
		...options
	};

	if (options.expires instanceof Date) {
		options.expires = options.expires.toUTCString();
	}

	let updatedCookie = encodeURIComponent(name) + "=" + encodeURIComponent(value);

	for (var optionKey in options) {
		updatedCookie += "; " + optionKey;
		let optionValue = options[optionKey];
		if (optionValue !== true) {
			updatedCookie += "=" + optionValue;
		}
	}

	document.cookie = updatedCookie;
}

function switchTheme() {

	let theme = getCookie('Theme');
	let newTheme;

	if (theme === 'dark') {
		newTheme = 'light';
	} else {
		newTheme = 'dark';
	}

	setCookie('Theme', newTheme);

	let btn = document.querySelector('#theme-switcher');

	if (btn) {
		btn.innerHTML = newTheme === 'dark' ? '<i class="fa-solid fa-moon">' : '<i class="fa-solid fa-sun"></i>';
	}

	document.body.setAttribute("data-mdb-theme", newTheme);

}
