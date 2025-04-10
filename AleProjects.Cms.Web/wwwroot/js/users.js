var vueAppOptions = {

	data() {
		return {
			pageHeight: "",
			drawer: true,
			profile: { name: "", avatar: "/images/empty-avatar.png" },
			navmenu: [],
			activeNavSection: "users",
			appVersion: null,

			users: [],
			roles: [],

			editedUserProps: false,
			editedUser: {
				id: 0,
				login: null,
				role: null,
				name: null,
				email: null,
				locale: null,
				apikey: null,
				isEnabled: false
			},

			resetApiKey: false,

			newUserProps: false,
			newUser: {
				login: null,
				role: null
			},

			invalidNewLogins: [],

			errors: {
				role: null,
				isEnabled: null,
			},

			deleteUserConfirm: false,
			userToDelete: 0,
		}
	},

	methods: {

		signout() {
			application.signOut();
		},

		getRolesList() {

			Quasar.LoadingBar.start();

			application
				.apiCallAsync("/api/v1/users/roles", "GET", null, { "Accept": "application/x-msgpack" }, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {
						this.roles = r.result;
					}
				});
		},

		getUsersList() {

			Quasar.LoadingBar.start();

			application
				.apiCallAsync("/api/v1/users", "GET", null, { "Accept": "application/x-msgpack" }, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {
						this.users = r.result;
					}
				});
		},

		startNewUser() {
			this.newUser = { login: null, role: this.roles.at(-1) };
			this.errors = { role: null, isEnabled: null };
			this.newUserProps = true;
		},

		createUser() {

			let dto = {
				login: this.newUser.login,
				role: this.newUser.role
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/users`, "POST", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.newUserProps = false;

						this.users.push(r.result);

						displayMessage(TEXT.USERS.get('MESSAGE_CREATE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.USERS.get('MESSAGE_CREATE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors) {

								if (r.result.errors.Login)
									this.$refs.NewUserLogin.validate();

								if (r.result.errors.Role)
									this.errors.role = TEXT.USERS.format('MESSAGE_INVALID_ROLE', this.roles.join(", "));

							} else {

								this.$refs.NewUserLogin.validate();

							}

						} else if (r.status == 409) {

							this.invalidNewLogins.push(dto.login);
							this.$refs.NewUserLogin.validate();
							this.$refs.NewUserLogin.focus();
						}
					}
				});
		},

		startUpdateUser(id) {

			this.errors = { role: null, isEnabled: null };
			this.resetApiKey = false;

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/users/${id}`, "GET", null, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {
						this.editedUser = r.result;
						this.editedUserProps = true;
					} else {
						displayMessage(`${formatHTTPStatus(r)}`, true);
					}
				});
		},

		updateUser() {

			let dto = {
				role: this.editedUser.role,
				name: this.editedUser.name,
				email: this.editedUser.email,
				isEnabled: this.editedUser.isEnabled,
				locale: this.editedUser.locale,
				resetApiKey: this.resetApiKey
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/users/${this.editedUser.id}`, "PUT", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.editedUserProps = false;

						for (let i = 0, n = this.users.length; i < n; i++)
							if (this.users[i].id == this.editedUser.id) {
								this.users[i] = r.result;
								break;
							}

						displayMessage(TEXT.USERS.get('MESSAGE_UPDATE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.USERS.get('MESSAGE_UPDATE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors) {

								if (r.result.errors.Role)
									this.errors.role = TEXT.USERS.format('MESSAGE_INVALID_ROLE_UPDATE', this.roles.join(", "));

								if (r.result.errors.Name)
									this.$refs.UserName.validate();

								if (r.result.errors.Email)
									this.$refs.UserEmail.validate();


								if (r.result.errors.Locale)
									this.$refs.UserLocale.validate();

								if (r.result.errors.IsEnabled)
									this.errors.isEnabled = true;

							} else {

								this.$refs.UserName.validate();
								this.$refs.UserEmail.validate();
								this.$refs.UserLocale.validate();

							}

						}
					}
				});

		},

		startDeleteUser(id) {
			this.userToDelete = id;
			this.deleteUserConfirm = true;
		},

		deleteUser() {

			this.deleteUserConfirm = false;

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/users/${this.userToDelete}`, "DELETE", null, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						for (let i = 0, n = this.users.length; i < n; i++)
							if (this.users[i].id == this.userToDelete) {
								this.users.splice(i, 1);
								break;
							}

						displayMessage(TEXT.USERS.get('MESSAGE_DELETE_SUCCESS'), false);

						if (r.result.signout)
							this.signout();

					} else {

						if (r.status == 400) {
							displayMessage(TEXT.USERS.get('MESSAGE_DELETE_IMPOSSIBLE'), true);
						} else {
							displayMessage(`${TEXT.USERS.get('MESSAGE_DELETE_FAIL')} (${formatHTTPStatus(r)})`, true);
						}
					}
				});
		},

		onRoleChanged(value) {
			if (value)
				this.errors.role = null;
			else
				this.errors.role = TEXT.USERS.format('MESSAGE_INVALID_ROLE', this.roles.join(", "));
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
					this.appVersion = r.result.status.version;
				}

			});

		this.getRolesList();
		this.getUsersList();

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
