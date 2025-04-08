var vueAppOptions = {

	data() {
		return {
			pageHeight: "",
			drawer: true,
			profile: { name: "", avatar: "/images/empty-avatar.png" },
			navmenu: [],
			activeNavSection: "events",

			destinations: [],
			destinationTypes: [
				{ value: "webhook", label: "Webhook" },
				{ value: "redis", label: "Redis pub/sub" },
				{ value: "rabbitmq", label: "RabbitMQ" }
			],

			editedDestinationProps: false,
			editedDestination: {
				id: 0,
				name: null,
				type: null,
				triggeringPath: null,
				triggeringPathAux: null,
				enabled: false,
				webhook: {
				},
				redis: {
				},
				rabbitMq: {
				}
			},

			revealPassword: false,

			newDestinationProps: false,
			newDestination: {
				name: null,
				type: null
			},

			deleteDestinationConfirm: false,
			destinationToDelete: 0,
		}
	},

	methods: {

		signout() {
			application.signOut();
		},

		typeIcon(type) {

			switch (type) {
				case "webhook":
					return "/images/webhook.svg";
				case "redis":
					return "/images/redis.svg";
				case "rabbitmq":
					return "/images/rabbitmq.svg";
				default:
					return null;
			}

		},

		destinationIsNotOk() {

			let dest = this.editedDestination;

			return !dest.name || dest.name.trim() == ''
				|| (dest.type == 'webhook' && (!dest.webhook.endpoint || dest.webhook.endpoint.trim() == ''))
				|| (dest.type == 'redis' && (
					!dest.redis.endpoint || dest.redis.endpoint.trim() == ''
					|| !dest.redis.channel || dest.redis.channel.trim() == ''));

		},

		getDestinationsList() {

			Quasar.LoadingBar.start();

			application
				.apiCallAsync("/api/v1/eventdestinations", "GET", null, { "Accept": "application/x-msgpack" }, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {
						this.destinations = r.result;
					}
				});
		},

		startNewDestination() {
			this.newDestination = { name: null, type: "webhook" };
			this.newDestinationProps = true;
		},

		createDestination() {

			let dto = {
				name: this.newDestination.name,
				type: this.newDestination.type
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/eventdestinations`, "POST", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.newDestinationProps = false;

						this.destinations.push(r.result);

						displayMessage(TEXT.DESTINATIONS.get('MESSAGE_CREATE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.DESTINATIONS.get('MESSAGE_CREATE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors) {

								if (r.result.errors.Name)
									this.$refs.NewDestinationName.validate();

							} else {

								this.$refs.NewDestinationName.validate();

							}

						}
					}
				});


		},

		initializeDialog(data) {

			this.editedDestination = data;

			if (data.type == "webhook") {
				this.editedDestination.webhook.resetSecret = false;
			}

			this.revealPassword = false;
		},

		startUpdateDestination(id) {

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/eventdestinations/${id}`, "GET", null, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {
						this.initializeDialog(r.result);
						this.editedDestinationProps = true;
					} else {
						displayMessage(`${formatHTTPStatus(r)}`, true);
					}
				});

		},

		updateDestination() {

			let dto = {
				name: this.editedDestination.name,
				enabled: this.editedDestination.enabled,
				triggeringPath: this.editedDestination.triggeringPath,
				triggeringPathAux: this.editedDestination.triggeringPathAux,
			};

			switch (this.editedDestination.type) {

				case "webhook":

					dto.webhook = {
						endpoint: this.editedDestination.webhook.endpoint,
						resetSecret: this.editedDestination.webhook.resetSecret
					};

					break;

				case "redis":

					dto.redis = {
						endpoint: this.editedDestination.redis.endpoint,
						user: this.editedDestination.redis.user,
						password: this.editedDestination.redis.password,
						channel: this.editedDestination.redis.channel
					};

					break;

				case "rabbitmq":

					dto.rabbitMq = {
						hostName: this.editedDestination.rabbitMq.hostName,
						user: this.editedDestination.rabbitMq.user,
						password: this.editedDestination.rabbitMq.password,
						exchange: this.editedDestination.rabbitMq.exchange,
						exchangeType: this.editedDestination.rabbitMq.exchangeType,
						routingKey: this.editedDestination.rabbitMq.routingKey
					};

					break;
			}


			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/eventdestinations/${this.editedDestination.id}`, "PUT", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.editedDestinationProps = false;

						for (let i = 0, n = this.destinations.length; i < n; i++)
							if (this.destinations[i].id == this.editedDestination.id) {
								this.destinations[i] = r.result;
								break;
							}

						displayMessage(TEXT.DESTINATIONS.get('MESSAGE_UPDATE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.DESTINATIONS.get('MESSAGE_UPDATE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors) {

								if (r.result.errors.Name)
									this.$refs.DestinationName.validate();

								if (r.result.errors.hasOwnProperty("Webhook.Endpoint"))
									this.$refs.DestinationWebhookEndpoint.validate();

								if (r.result.errors.hasOwnProperty("Redis.Endpoint"))
									this.$refs.DestinationRedisEndpoint.validate();

								if (r.result.errors.hasOwnProperty("Redis.Channel"))
									this.$refs.DestinationRedisChannel.validate();

							} else {

								this.$refs.DestinationName.validate();

							}

						}
					}
				});

		},

		startDeleteDestination(id) {
			this.destinationToDelete = id;
			this.deleteDestinationConfirm = true;
		},

		deleteDestination() {

			this.deleteDestinationConfirm = false;

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/eventdestinations/${this.destinationToDelete}`, "DELETE", null, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						for (let i = 0, n = this.destinations.length; i < n; i++)
							if (this.destinations[i].id == this.destinationToDelete) {
								this.destinations.splice(i, 1);
								break;
							}

						displayMessage(TEXT.DESTINATIONS.get('MESSAGE_DELETE_SUCCESS'), false);

					} else {
						displayMessage(`${TEXT.DESTINATIONS.get('MESSAGE_DELETE_FAIL')} (${formatHTTPStatus(r)})`, true);
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
					this.profile = r.result.user;
					this.navmenu = r.result.menu;
				}

			});

		this.getDestinationsList();

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
