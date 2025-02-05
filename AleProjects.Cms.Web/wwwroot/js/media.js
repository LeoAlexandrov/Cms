var vueAppOptions = {

	data() {
		return {
			pageHeight: "",
			drawer: true,
			profile: { name: "", avatar: "/images/empty-avatar.png" },
			navmenu: [],
			activeNavSection: "media",

			folderLink: "",
			mediaEntries: [],
			path: [{ label: TEXT.MEDIA.get("ROOT"), link: null }],
			selected: [],
			opened: { name: null, link: null, hrefLink: null, width: 0, height: 0, size: 0, referencedBy: [] },

			maxUploadSize: 10 * 1024 * 1024,
			safeNameRegexString: "^[\\w-]+.\\w+$",

			entryProps: false,
			upload: false,
			selectedForUpload: null,

			deleteEntriesConfirm: false,

			newFolderProps: false,
			newFolderName: null,
		}
	},

	methods: {

		signout() {
			application.signOut();
		},

		copyToClipboard(text) {

			if (navigator.clipboard) {

				navigator.clipboard
					.writeText(text)
					.then(
						function () {
							displayMessage(TEXT.COMMON.get('MESSAGE_CLIPBOARD_SUCCESS'), false);
						},
						function (err) {
							displayMessage(TEXT.COMMON.get('MESSAGE_CLIPBOARD_FAIL'), true);
						});
			}
		},

		formatFileSize(size) {

			let sizeUnits = TEXT.COMMON.get("UNITS_FILESIZE");
			var result;


			if (size < 1024)
				result = size.toString() + " " + sizeUnits[0];
			else if (size < 1048576)
				result = (size / 1024).toFixed(2) + " " + sizeUnits[1];
			else if (size < 1073741824) // 1048576000
			{
				var N = size / 1048576;

				if (N < 10)
					result = N.toFixed(2) + " " + sizeUnits[2];
				else if (N < 100)
					result = N.toFixed(1) + " " + sizeUnits[2];
				else
					result = N.toFixed("F0") + " " + sizeUnits[2];
			}
			else
				result = (size / 1073741824).toFixed(1) + " " + sizeUnits[3]; //  1048576000F

			return result.replace('.', TEXT.COMMON.get("DECIMAL_SEPARATOR"));
		},

		uploadHint() {
			return TEXT.MEDIA.format("MESSAGE_MAXIMUM_UPLOAD_SIZE_HINT", this.formatFileSize(this.maxUploadSize));
		},

		openImage(link) {
			window.open(`/api/v1/media/entry?link=${link}`, '_blank', 'popup');
		},

		readFolder(link, pushState) {

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/media/folder?link=${link}`, "GET", null, { "Accept": "application/x-msgpack" }, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.folderLink = link;
						this.selected = [];
						this.mediaEntries = r.result.entries;

						r.result.path[0].label = TEXT.MEDIA.get("ROOT");
						this.path = r.result.path

						if (pushState)
							window.history.pushState({ folderLink: link }, "", `/media/${link}`);
					} else {
						displayMessage(`${TEXT.MEDIA.get('MESSAGE_READFOLDER_FAIL')} (${formatHTTPStatus(r)})`, true);
					}
				});

		},

		getRefs(link) {

			this.opened.referencedBy = [];

			application
				.apiCallAsync(`/api/v1/documents/mediarefs?link=${link}`, "GET", null, { "Accept": "application/x-msgpack" }, null)
				.then((r) => {

					if (r.ok) {
						this.opened.referencedBy = r.result;
					}
				});

		},

		getProperties(link) {

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/media/properties?link=${link}`, "GET", null, { "Accept": "application/x-msgpack" }, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {
						this.opened = r.result;
						this.opened.hrefLink = `^('${r.result.link}')`;

						this.getRefs(link);

						this.entryProps = true;


					} else {
						displayMessage(`${TEXT.MEDIA.get('MESSAGE_PROPERTIES_FAIL')} (${formatHTTPStatus(r)})`, true);
					}
				});

		},


		onEntryClicked(e) {

			if (e.isFolder) {
				this.readFolder(e.link, true);
			} else {
				this.getProperties(e.link);
			}

		},

		startUpload() {
			this.selectedForUpload = null;
			this.upload = true;
		},

		uploadFile() {

			let form = document.getElementById("upload_form");
			let input = form.querySelector('input[type="file"]');
			let data = new FormData();

			data.append("destination", this.folderLink);

			for (let i = 0; i < input.files.length; i++)
				data.append("file", input.files[i]);

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/media/upload`, "POST", data, null, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {
						this.upload = false;
						this.selectedForUpload = null;

						let hashset = new Set(r.result.map((e) => e.link));
						let newEntries = this.mediaEntries.filter((e) => !hashset.has(e.link));

						for (const e of r.result)
							newEntries.push(e);

						this.mediaEntries = newEntries;

						if (r.result.length == input.files.length)
							displayMessage(TEXT.MEDIA.get('MESSAGE_UPLOAD_SUCCESS'), false);
						else
							displayMessage(TEXT.MEDIA.get('MESSAGE_UPLOAD_PARIAL_FAIL'), true);

					} else {
						displayMessage(`${TEXT.MEDIA.get('MESSAGE_UPLOAD_FAIL')} (${formatHTTPStatus(r)})`, true);
					}
				});
		},

		onUploadRejected(rejectedEntries) {
			displayMessage(TEXT.MEDIA.get('MESSAGE_UPLOAD_DENIED'), true);
		},

		startCreateFolder() {
			this.newFolderProps = true;
		},

		createFolder() {

			let dto = { name: this.newFolderName, destination: this.folderLink };

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/media/folder`, "POST", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.newFolderProps = false;
						this.newFolderName = null;
						this.mediaEntries.push(r.result);

						displayMessage(TEXT.MEDIA.get('MESSAGE_FOLDERCREATE_SUCCESS'), false);

					} else {
						displayMessage(`${TEXT.MEDIA.get('MESSAGE_FOLDERCREATE_FAIL')} (${formatHTTPStatus(r)})`, true);
					}
				});

		},

		startDelete() {
			this.deleteEntriesConfirm = true;
		},

		deleteEntries() {

			this.deleteEntriesConfirm = false;

			let dto = { links: this.selected };

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/media/entry`, "DELETE", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						let hashset = new Set(r.result);
						let newEntries = this.mediaEntries.filter((e) => !hashset.has(e.link));

						this.mediaEntries = newEntries;

						if (r.result.length == dto.links.length) {

							this.selected = [];
							displayMessage(TEXT.MEDIA.get('MESSAGE_DELETE_SUCCESS'), false);

						} else {

							let newSelection = this.selected.filter((e) => !hashset.has(e));
							this.selected = newSelection;

							displayMessage(TEXT.MEDIA.get('MESSAGE_DELETE_PARTIAL_FAIL'), true);
						}

					} else {
						displayMessage(`${TEXT.MEDIA.get('MESSAGE_DELETE_FAIL')} (${formatHTTPStatus(r)})`, true);
					}
				});

		},

		validateFileName(val) {

			if (!val)
				return true;

			let re = new RegExp(this.safeNameRegexString);

			for (const v of val)
				if (!re.test(v.name))
					return false;

			return true;
		}

	},

	computed: {

		fileExists() {

			if (this.selectedForUpload != null) {

				let name = this.selectedForUpload.name;

				for (const e of this.mediaEntries)
					if (e.name == name)
						return true;

			}

			return false;
		},

		statusLine() {

			let n = this.mediaEntries.length;
			let m = this.selected.length;

			if (m == 0)
				return `${n} ${TEXT.MEDIA.plural(n, "LABEL_ITEM")}`;

			return `${n} ${TEXT.MEDIA.plural(n, "LABEL_ITEM")} | ${m} ${TEXT.MEDIA.plural(m, "LABEL_ITEM_SELECTED")}`;
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

		let qs = document.querySelector("#folder_link");
		let link = JSON.parse(qs.innerHTML);

		this.readFolder(link, true);

		qs = document.querySelector("#upload_params");

		let uploadParams = JSON.parse(qs.innerHTML);

		this.maxUploadSize = uploadParams.maxUploadSize;
		this.safeNameRegexString = uploadParams.safeNameRegexString;

		window.onpopstate = (e) => {

			var state = e.state;

			if (state == null) {
				window.history.back();
				return;
			}

			if (state.hasOwnProperty("folderLink")) {
				this.readFolder(state["folderLink"], false);
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