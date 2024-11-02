var vueAppOptions = {

	data() {
		return {
			pageHeight: "",
			drawer: true,
			profile: { name: "", avatar: "/images/empty-avatar.png" },
			navmenu: [],
			activeNavSection: "schemata",
			splitter: 20,

			schemata: [],
			selectedSchema: 0,

			editedSchema: {
				id: 0,
				namespace: null,
				description: null,
				data: null,
				modifiedAt: null
			},

			hasChanged: false,

			newSchemaProps: false,
			newSchema: {
				description: null,
			},

			errorSchemaDisplay: false,
			errorSchemaContent: null,

			deleteSchemaConfirm: false,
			unsavedSchemaConfirm: false,
			unsavedSchemaAction: null,

		}
	},

	methods: {

		signout() {

			if (this.hasChanged) {
				this.confirmUnsavedDoc("signout", null);
				return;
			}

			application.signOut();
		},

		getSchemataList(id) {

			Quasar.LoadingBar.start();

			application
				.apiCallAsync("/api/v1/schemata", "GET", null, { "Accept": "application/x-msgpack" }, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {
						this.schemata = r.result;
					}
				});
		},

		getEmptySchema() {
			return {
				id: 0,
				namespace: null,
				description: null,
				data: null,
				modifiedAt: null
			};
		},


		selectSchema(id, pushState, ignoreChanges, selectList) {

			if (this.hasChanged && !ignoreChanges) {
				this.confirmUnsavedSchema("select", id);
				return;
			}

			if (id == 0) {

				this.selectedSchema = 0;
				this.editedSchema = this.getEmptySchema();
				this.hasChanged = false;

				if (pushState)
					window.history.pushState({ schemaId: 0 }, "", `/schemata`);

				return;
			}

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/schemata/${id}`, "GET", null, { "Accept": "application/x-msgpack" }, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.editedSchema = r.result;
						this.hasChanged = false;

						if (selectList)
							this.selectedSchema = id;

						if (pushState)
							window.history.pushState({ schemaId: id }, "", `/schemata/${id}`);

					}
				});
		},

		onSchemaSelected(id) {
			this.selectSchema(id, true, false, true);
		},

		startNewSchema(ignoreChanges) {

			if (this.hasChanged && !ignoreChanges) {
				this.confirmUnsavedSchema("create", null);
				return;
			}

			this.newSchema = {
				description: null
			}

			this.newSchemaProps = true;

			Vue.nextTick(() => {
				this.$refs.NewSchemaDescription.focus();
				this.$refs.NewSchemaDescription.select();
			});
		},

		createSchema() {

			let dto = {
				description: this.newSchema.description
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/schemata`, "POST", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.editedSchema = r.result;
						this.hasChanged = false;
						this.newSchemaProps = false;

						this.schemata.push(this.editedSchema);
						this.selectedSchema = this.editedSchema.id;

						window.history.pushState({ schemaId: this.selectedSchema }, "", `/schemata/${this.selectedSchema}`);

						displayMessage(TEXT.SCHEMATA.get('MESSAGE_CREATE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.SCHEMATA.get('MESSAGE_CREATE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors) {

								if (r.result.errors.Description)
									this.$refs.NewSchemaDescription.validate();

							} else {

								this.$refs.NewSchemaDescription.validate();

							}

						} else if (r.status == 409) {

							this.$refs.NewSchemaDescription.validate();
						}
					}
				});

		},

		updateSchema(onlySave) {

			let dto = {
				description: this.editedSchema.description,
				onlySave: onlySave,
				data: this.editedSchema.data
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/schemata/${this.selectedSchema}`, "PUT", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.editedSchema = r.result;
						this.hasChanged = false;

						for (let i = 0, n = this.schemata.length; i < n; i++)
							if (this.schemata[i].id == this.selectedSchema) {
								this.schemata[i].description = r.result.description;
								this.schemata[i].namespace = r.result.namespace;
								this.schemata[i].modifiedAt = r.result.modifiedAt;
								break;
							}

						displayMessage(TEXT.SCHEMATA.get('MESSAGE_UPDATE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.SCHEMATA.get('MESSAGE_UPDATE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors) {

								if (r.result.errors.Description)
									this.$refs.NewSchemaDescription.validate();
								else if (r.result.errors.Data) {
									this.errorSchemaContent = r.result.errors.Data[0];
									this.errorSchemaDisplay = true;
								}

							} else {

								this.$refs.NewSchemaDescription.validate();

							}

						} else if (r.status == 409) {

							this.$refs.NewSchemaDescription.validate();
						}
					}
				});
		},

		startDeleteSchema() {
			this.deleteSchemaConfirm = true;
		},

		deleteSchema() {

			this.deleteSchemaConfirm = false;

			let id = this.selectedSchema;

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/schemata/${id}`, "DELETE", null, null, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						for (let i = 0, n = this.schemata.length; i < n; i++)
							if (this.schemata[i].id == id) {
								this.schemata.splice(i, 1);
								break;
							}

						window.history.replaceState({ schemaId: 0 }, "", `/schemata`);

						this.selectSchema(0, false, true, false);

						displayMessage(TEXT.SCHEMATA.get('MESSAGE_DELETE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.SCHEMATA.get('MESSAGE_DELETE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors) {

								if (r.result.errors.Id) {
									this.errorSchemaContent = r.result.errors.Id[0];
									this.errorSchemaDisplay = true;
								}

							}

						}
					}
				});
		},

		compileAndReload() {

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/schemata/compile`, "POST", null, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						displayMessage(TEXT.SCHEMATA.get('MESSAGE_COMPILE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.SCHEMATA.get('MESSAGE_COMPILE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors && r.result.errors.Data) {
								this.errorSchemaContent = r.result.errors.Data[0];
								this.errorSchemaDisplay = true;
							}

						}
					}
				});

		},

		discardSchema() {

			if (this.editedSchema.id) {
				this.hasChanged = false;
				this.selectSchema(this.editedSchema.id, false, true, false);
			} else {
				this.selectSchema(0, false, true, false);
			}

		},

		confirmUnsavedSchema(action, param) {
			this.unsavedSchemaAction = { action: action, param: param };
			this.unsavedSchemaConfirm = true;
		},

		confirmStay() {
			this.selectedSchema = this.editedSchema.id;
			this.unsavedSchemaAction = null;
			this.unsavedSchemaConfirm = false;
		},

		confirmDiscard() {
			let action = this.unsavedSchemaAction;

			this.unsavedSchemaAction = null;
			this.unsavedSchemaConfirm = false;

			if (action) {
				switch (action.action) {
					case "create":
						this.startNewSchema(true);
						break;
					case "select":
						this.hasChanged = false;
						this.selectSchema(action.param, true, true, true);
						break;
					case "signout":
						this.hasChanged = false;
						this.signout();
						break;
				}
			}
		},

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

		let id = document.querySelector("#schema_id");

		if (id)
			id = JSON.parse(id.innerHTML);

		this.getSchemataList(id);

		if (id)
			this.selectSchema(id, true, true, true);
		else
			window.history.pushState({ schemaId: 0 }, "", `/schemata`);


		window.onpopstate = (e) => {

			var state = e.state;

			if (state == null) {
				window.history.back();
				return;
			}

			if (state.hasOwnProperty("schemaId")) {
				this.selectSchema(parseInt(state["schemaId"]), false, true, true);
			}
		};


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

	},

	components: {
		'code-editor': CodeEditor
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
