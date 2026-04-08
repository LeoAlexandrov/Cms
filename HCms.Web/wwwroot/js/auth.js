var vueAppOptions = {
	data() {
		return {
			ldapLogin: null,
			ldapPassword: null,
			isLdapPassword: true,
			ldapCredentials: false,
			cfTurnstile: false
		}
	},

	methods: {

		showLdap() {
			this.ldapLogin = null;
			this.ldapPassword = null;
			this.ldapCredentials = true;
		},

		submitLdap() {
			let state = document.querySelector('input[name="__LdapState"]');
			document.forms["ldap_cred_form"].submit();
		},

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

		let ldapStart = document.querySelector('#start_ldap');

		if (ldapStart)
			this.ldapCredentials = true;

	}

}