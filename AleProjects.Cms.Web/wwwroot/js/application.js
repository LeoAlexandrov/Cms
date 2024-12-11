var application = {

	_root: null,
	_theme: null,
	_refreshInProgress: false,
	_bcRefresh: new BroadcastChannel('jwt_refresh_channel'),

	getRoot: function () {

		if (this._root != null)
			return this._root;

		let scripts = document.getElementsByTagName('script');
		let url;

		for (let i = 0, n = scripts.length; i < n; i++)
			if (scripts[i].src) {
				url = new URL(scripts[i].src);

				if (url.pathname.endsWith('/js/application.js')) {
					this._root = url.pathname.substring(0, url.pathname.length - '/js/application.js'.length);
					break;
				}
			}

		return this._root;
	},

	withRoot: function (url) {

		if (/(http(s?)):\/\//i.test(url))
			return url;

		if (this._root != null)
			return this._root + url;

		return this.getRoot() + url;
	},

	authEndpoints: () => JSON.parse(document.querySelector("#auth_endpoints").innerHTML),

	signOut: function () {

		let authEndpoints = this.authEndpoints();

		this
			.apiCallAsync(authEndpoints.signout, 'POST', null, null, null)
			.then((r) => {
				if (r.ok)
					window.location = authEndpoints.signin;
			});
	},

	onBroadcastMessage: function (msgEvent) {

		this._refreshInProgress = msgEvent.data !== 0;

	},

	apiCallAsync: async function (url, requestMethod, data, headers, contentType) {

		let fetchParams = { method: requestMethod, mode: "same-origin", headers: { ...headers } };
		let authEndpoints = this.authEndpoints();
		let cType = null;

		if (requestMethod != "GET") {

			if (contentType) {
				cType = contentType;
				fetchParams.headers["Content-Type"] = cType;
			}

			let csrf_token = document.querySelector('input[name="__RequestVerificationToken"]').value;

			fetchParams.headers["X-RequestVerificationToken"] = csrf_token;


			if (data) {
				if (data instanceof FormData) {

					fetchParams["body"] = data;

				} else if (cType == "application/x-www-form-urlencoded" || cType == "multipart/form-data") {

					let form = new FormData();

					for (var key in data)
						form.append(key, data[key]);


					fetchParams["body"] = form;

				} else if (cType == "application/x-msgpack") {

					fetchParams["body"] = MessagePack.encode(data);

				} else if (!cType || cType.startsWith("application/json")) {

					fetchParams.headers["Content-Type"] = "application/json";

					if (data instanceof String)
						fetchParams["body"] = data.toString();
					else
						fetchParams["body"] = JSON.stringify(data);

				}
			}
		}

		let requestResponse;
		let result;
		let totalItems = 0;
		let link = null;
		let count401 = 0;
		let maxRefreshAttempts = 2;

		do {
			requestResponse = await fetch(this.withRoot(url), fetchParams);

			if (requestResponse.status == 401) {

				count401++;

				let refreshFailed = false;

				if (count401 > maxRefreshAttempts) {

					refreshFailed = true;

				} else if (!this._refreshInProgress) {

					this._bcRefresh.postMessage(1);
					this._refreshInProgress = true;

					let refreshResponse = await fetch(authEndpoints.refresh,
						{
							method: "POST",
							headers: { "Content-Type": "application/json; charset=utf-8" }
						});

					this._refreshInProgress = false;
					this._bcRefresh.postMessage(0);

					if (!refreshResponse.ok) 
						refreshFailed = true;

				} else {

					let maxWaitCycles = 20;
					let resolver = (resolve) => setTimeout(() => resolve(), 1000);
					let refreshing;

					while (this._refreshInProgress && maxWaitCycles-- > 0) {
						refreshing = new Promise(resolver);
						await refreshing;
					}

					if (this._refreshInProgress)
						refreshFailed = true;

				}

				if (refreshFailed) {

					if (requestMethod == "POST" || requestMethod == "PUT" || requestMethod == "PATCH" || requestMethod == "DELETE") {

						let wnd = window.open(authEndpoints.signin, '_blank', 'popup');

						if (wnd) {
							this.setCookie("popup_auth", "1");
							return { ok: false, status: 401, result: null, contentType: requestResponse.headers.get('Content-Type'), totalItems: 0, link: null };
						}
					}

					if (authEndpoints) 
						window.location = authEndpoints.signin;

					return { ok: false, status: 401, result: null, contentType: null, totalItems: 0, link: null };
				}

			} else {

				count401 = 0;

				if (requestResponse.ok || requestResponse.status == 400 || requestResponse.status == 409) {

					cType = requestResponse.headers.get('Content-Type');
					totalItems = requestResponse.headers.get('Total-items-count');
					link = requestResponse.headers.get('Link');

					if (!cType)
						result = null;
					else if (cType.startsWith("application/json") || cType.startsWith("application/problem+json"))
						result = await requestResponse.json();
					else if (cType == "application/x-msgpack")
						result = await MessagePack.decodeAsync(requestResponse.body);
					else if (cType == "application/x-www-form-urlencoded")
						result = await requestResponse.formData();
					else
						result = await requestResponse.text();

				} else {

					result = null;
					cType = null;
					totalItems = 0;
					link = null;

				}

			}

		} while (count401 != 0);

		return { ok: requestResponse.status >= 200 && requestResponse.status < 300, status: requestResponse.status, result: result, contentType: cType, totalItems: totalItems, link: link };
	},

	getCookie: function (name) {
		let matches = document.cookie.match(new RegExp("(?:^|; )" + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + "=([^;]*)"));
		return matches ? decodeURIComponent(matches[1]) : null;
	},

	setCookie: function (name, value, options = {}) {

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
	},

	deleteCookie: function (name) {
		this.setCookie(name, "", { 'max-age': -1 })
	},

	setTitle: function (title) {
		document.title = title + " - The Headless CMS";
		return title;
	},

	isDarkTheme: function () {

		if (!this._theme)
			this._theme = this.getCookie("AppTheme");

		return this._theme == "dark";
	},

	setAppTheme: function (isDark) {
		this._theme = isDark ? "dark" : "light";
		this.setCookie("AppTheme", this._theme);
	}
};


application._bcRefresh.onmessage = application.onBroadcastMessage;