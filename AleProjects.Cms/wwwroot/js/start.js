var vueAppOptions = {

	data() {
		return {
			account: null
		}
	},

	methods: {

	},

	mounted() {

		document.querySelector("body").classList.remove("body-progress");

		var acc = document.querySelector("#account");

		if (acc)
			acc = JSON.parse(account.innerHTML);

		this.account = acc;


		Vue.nextTick(() => {
			this.$refs.OwnerAccount.focus();
		});

	}

}