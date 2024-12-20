var vueAppOptions = {

	data() {
		return {
			drawer: true,
			profile: { name: "", avatar: "/images/empty-avatar.png" },
			navmenu: [],
			activeNavSection: "webhooks"
		}
	},

	methods: {

		signout() {
			application.signOut();
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

	}

}