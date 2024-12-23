function TextDictionary(dict) {
	var dictionary = dict;

	this.get = function (key) {
		return dictionary[key];
	}

	this.list = function () {
		let args = arguments;
		let result = {};
		let k;

		for (let i = 0, len = args.length; i < len; i++) {

			k = args[i].indexOf('/');

			if (k < 0)
				result[args[i]] = dictionary[args[i]];
			else
				result[args[i].substr(k + 1)] = dictionary[args[i].substr(0, k)];
		}

		return result;
	}

	this.format = function () {
		let args = arguments;
		let key = args[0];
		let str = dictionary[key];
		let params = [];

		for (let i = 1, len = args.length; i < len; i++)
			params.push(args[i]);

		if (!str)
			return "";

		return str.replace(/%s[0-9]+/g, function (matchedStr) {
			const variableIndex = matchedStr.replace("%s", "") - 1;
			return params[variableIndex];
		});
	}

	this.plural = function (n, key) {

		let words = dictionary[key];

		if (!Array.isArray(words))
			return words;

		if (words.length == 0)
			return key;

		if (words.length == 1 || n == 1)
			return words[0];

		if (words.length == 2)
			return words[1];

		let rem100 = n % 100;
		let rem10 = n % 10;

		if (rem100 == 11 || rem100 == 12 || rem100 == 13 || rem100 == 14 ||
			rem10 == 0 || rem10 == 5 || rem10 == 6 || rem10 == 7 || rem10 == 8 || rem10 == 9)
			return words[1];

		if (rem10 == 2 || rem10 == 3 || rem10 == 4)
			return words[2];

		return words[3];
	}
}

function TEXT() { }

TEXT.COMMON = new TextDictionary(DICTIONARY_COMMON);
TEXT.DOCS = new TextDictionary(DICTIONARY_DOCS);
TEXT.MEDIA = new TextDictionary(DICTIONARY_MEDIA);
TEXT.SCHEMATA = new TextDictionary(DICTIONARY_SCHEMATA);
TEXT.USERS = new TextDictionary(DICTIONARY_USERS);
TEXT.WEBHOOKS = new TextDictionary(DICTIONARY_WEBHOOKS);