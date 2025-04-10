var vueAppOptions = {
	data() {
		return {
			cfTurnstile: false
		}
	},

	methods: {

		showTurnstile() {
			this.cfTurnstile = true;

			let cfSiteKey = JSON.parse(document.querySelector("#cf_sitekey").innerHTML);

			Vue.nextTick(() => {
				turnstile.render("#cf_turnstile", {
					sitekey: cfSiteKey,
					callback: function (token) {
						document.forms["cf_turnstile_form"].submit();
					}
				});
			});
		}
	},

	mounted() {
		document.querySelector("body").classList.remove("body-progress");
	}

}