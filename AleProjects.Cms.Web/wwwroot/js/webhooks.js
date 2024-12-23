var vueAppOptions = {

	data() {
		return {
			pageHeight: "",
			drawer: true,
			profile: { name: "", avatar: "/images/empty-avatar.png" },
			navmenu: [],
			activeNavSection: "webhooks",

			webhooks: [],

			editedWebhookProps: false,
			editedWebhook: {
				id: 0,
				endpoint: null,
				secret: null,
				rootDocument: 0,
				enabled: false
			},

			resetSecret: false,

			newWebhookProps: false,
			newWebhook: {
				endpoint: null,
				rootDocument: 0
			},

			invalidNewEndpoints: [],

			errors: {
				role: null,
				isEnabled: null,
			},

			deleteWebhookConfirm: false,
			webhookToDelete: 0,
		}
	},

	methods: {

		signout() {
			application.signOut();
		},

		getWebhooksList() {

			Quasar.LoadingBar.start();

			application
				.apiCallAsync("/api/v1/webhooks", "GET", null, { "Accept": "application/x-msgpack" }, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {
						this.webhooks = r.result;
					}
				});
		},

		startNewWebhook() {
			this.newWebhook = { endpoint: null, rootDocument: 0 };
			this.newWebhookProps = true;
		},

		createWebhook() {

			let dto = {
				endpoint: this.newWebhook.endpoint,
				rootDocument: this.newWebhook.rootDocument
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/webhooks`, "POST", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.newWebhookProps = false;

						this.webhooks.push(r.result);

						displayMessage(TEXT.WEBHOOKS.get('MESSAGE_CREATE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.WEBHOOKS.get('MESSAGE_CREATE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors) {

								if (r.result.errors.Endpoint)
									this.$refs.NewWebhookEndpoint.validate();

								if (r.result.errors.NewWebhookRootDocument)
									this.$refs.NewWebhookRootDocument.validate();

							} else {

								this.$refs.NewWebhookEndpoint.validate();

							}

						}
					}
				});


		},

		startUpdateWebhook(id) {

			this.resetSecret = false;

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/webhooks/${id}`, "GET", null, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {
						this.editedWebhook = r.result;
						this.editedWebhookProps = true;
					} else {
						displayMessage(`${formatHTTPStatus(r)}`, true);
					}
				});

		},

		updateWebhook() {

			let dto = {
				endpoint: this.editedWebhook.endpoint,
				rootDocument: this.editedWebhook.rootDocument,
				resetSecret: this.resetSecret,
				enabled: this.editedWebhook.enabled
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/webhooks/${this.editedWebhook.id}`, "PUT", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.editedWebhookProps = false;

						for (let i = 0, n = this.webhooks.length; i < n; i++)
							if (this.webhooks[i].id == this.editedWebhook.id) {
								this.webhooks[i] = r.result;
								break;
							}

						displayMessage(TEXT.WEBHOOKS.get('MESSAGE_UPDATE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.WEBHOOKS.get('MESSAGE_UPDATE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors) {

								if (r.result.errors.Endpoint)
									this.$refs.WebhookEndpoint.validate();

								if (r.result.errors.RootDocument)
									this.$refs.WebhookRootDocument.validate();

							} else {

								this.$refs.WebhookEndpoint.validate();
								this.$refs.WebhookRootDocument.validate();

							}

						}
					}
				});

		},

		startDeleteWebhook(id) {
			this.webhookToDelete = id;
			this.deleteWebhookConfirm = true;
		},

		deleteWebhook() {

			this.deleteWebhookConfirm = false;

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/webhooks/${this.webhookToDelete}`, "DELETE", null, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						for (let i = 0, n = this.webhooks.length; i < n; i++)
							if (this.webhooks[i].id == this.webhookToDelete) {
								this.webhooks.splice(i, 1);
								break;
							}

						displayMessage(TEXT.WEBHOOKS.get('MESSAGE_DELETE_SUCCESS'), false);

					} else {
						displayMessage(`${TEXT.WEBHOOKS.get('MESSAGE_DELETE_FAIL')} (${formatHTTPStatus(r)})`, true);
					}
				});
		}
	},

	mounted() {

		document.querySelector("body").classList.remove("body-progress");

		application
			.apiCallAsync("/api/v1/ui/navigationmenu", "GET", null, null, null)
			.then((r) => {

				if (r.ok) {
					this.navmenu = r.result.menu;
				}

			});

		this.getWebhooksList();

		var setPageHeight = () => {
			let page = document.getElementById('top-page');

			if (page) {
				let style = window.getComputedStyle(page);
				let minH = style.getPropertyValue('min-height');
				this.pageHeight = minH;
			}
		};

		setTimeout(setPageHeight, 50);
		var timeout = false;

		window.addEventListener('resize', () => {
			clearTimeout(timeout);
			timeout = setTimeout(setPageHeight, 25);
		});

	}

}

function formatHTTPStatus(r) {

	if (r.status == 400) {
		if (r.result.errors && r.result.errors.antiforgery_token)
			return `${TEXT.COMMON.get("MESSAGE_ANTIFORGERY")}`;

		return `${TEXT.COMMON.get("MESSAGE_BADREQUEST")}: ${TEXT.COMMON.get("LABEL_HTTP_STATUS")} ${r.status}`;
	}

	if (r.status == 401)
		return `${TEXT.COMMON.get("MESSAGE_UNAUTHORIZED")}: ${TEXT.COMMON.get("LABEL_HTTP_STATUS")} ${r.status}`;

	if (r.status == 403)
		return `${TEXT.COMMON.get("MESSAGE_FORBIDDEN")}: ${TEXT.COMMON.get("LABEL_HTTP_STATUS")} ${r.status}`;

	if (r.status == 404)
		return `${TEXT.COMMON.get("MESSAGE_NOTFOUND")}: ${TEXT.COMMON.get("LABEL_HTTP_STATUS")} ${r.status}`;

	if (r.status == 409)
		return `${TEXT.COMMON.get("MESSAGE_CONFLICT")}: ${TEXT.COMMON.get("LABEL_HTTP_STATUS")} ${r.status}`;

	return `${TEXT.COMMON.get("LABEL_HTTP_STATUS")} ${r.status}`;
}

function displayMessage(messageText, attention) {

	let color;

	if (attention)
		color = "negative";
	else if (attention === false)
		color = "positive";
	else
		color = "dark";

	Quasar.Notify.create({ message: messageText, color: color });
}
