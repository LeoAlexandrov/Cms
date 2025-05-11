var DICTIONARY_COMMON = {
	'LOCALE': 'fr-FR',
	'LANG': 'fr',

	'BUTTON_OK': 'OK',
	'BUTTON_CANCEL': 'Annuler',
	'BUTTON_CLOSE': 'Fermer',
	'BUTTON_NEW': 'Créer',
	'BUTTON_DELETE': 'Supprimer',
	'BUTTON_CONTINUE': 'Suivant',

	'THEME_LIGHT': 'Thème lumière',
	'THEME_DARK': 'Thème sombre',

	'DECIMAL_SEPARATOR': '.',

	'LABEL_NO': 'Non',
	'LABEL_SELECTED': 'Choisi',
	'LABEL_OR_SEPARATOR': ' ou ',
	'LABEL_SIZE': 'Taille',
	'LABEL_HTTP_STATUS': 'Statut HTTP',

	'MESSAGE_OPERATION_IN_PROGRESS': 'Opération en cours...',
	'MESSAGE_WAIT': 'Attendez...',
	'MESSAGE_BADREQUEST': 'Paramètres de requête erronés',
	'MESSAGE_UNAUTHORIZED': 'Non autorisé',
	'MESSAGE_FORBIDDEN': 'Interdit',
	'MESSAGE_NOTFOUND': 'Objet non trouvé.',
	'MESSAGE_CONFLICT': 'L\'objet existe déjà.',
	'MESSAGE_ANTIFORGERY': 'Le jeton anti-contrefaçon n\'est pas valide. Rafraîchissez cette page.',
	'MESSAGE_CLIPBOARD_SUCCESS': "Copié avec succès dans le presse-papiers",
	'MESSAGE_CLIPBOARD_FAIL': "Échec de la copie dans le presse-papiers",

	'TITLE_WARNING': 'Avertissement',
	'TITLE_CONFIRM': 'Confirmer',

	'UNITS_FILESIZE': ['B', 'KB', 'MB', 'GB', 'TB'],
};

var DICTIONARY_DOCS = {
	"SLUG_NEW_DOCUMENT": "nouveau-document",
	"NAME_NEW_FRAGMENT": "nouveau-fragment",
	"MESSAGE_OPEN_FAIL": "Impossible d\'ouvrir le document.",
	"MESSAGE_CREATE_SUCCESS": "Le document a été créé avec succès.",
	"MESSAGE_CREATE_FAIL": "Échec de la création du document.",
	"MESSAGE_UPDATE_SUCCESS": "Le document a été mis à jour avec succès.",
	"MESSAGE_UPDATE_FAIL": "Échec de la mise à jour du document.",
	"MESSAGE_DELETE_SUCCESS": "Le document a été supprimé avec succès.",
	"MESSAGE_DELETE_FAIL": "Échec de la suppression du document.",
	"MESSAGE_DELETE_IMPOSSIBLE": "Ce document ne peut pas être supprimé.",
	"MESSAGE_LOCK_SUCCESS": "Le document a été verrouillé avec succès.",
	"MESSAGE_LOCK_FAIL": "Échec du verrouillage du document.",
	"MESSAGE_UNLOCK_SUCCESS": "Le document a été déverrouillé avec succès.",
	"MESSAGE_UNLOCK_FAIL": "Échec du déverrouillage du document.",
	"MESSAGE_CREATE_FR_SUCCESS": "Le fragment a été créé avec succès.",
	"MESSAGE_CREATE_FR_FAIL": "Échec de la création du fragment.",
	"MESSAGE_DELETE_FR_SUCCESS": "Le fragment a été supprimé avec succès.",
	"MESSAGE_DELETE_FR_FAIL": "Impossible de supprimer le fragment.",
	"MESSAGE_LOAD_FR_FAIL": "Impossible de charger le fragment.",
	"MESSAGE_UPDATE_FR_SUCCESS": "Le fragment a été mis à jour avec succès.",
	"MESSAGE_UPDATE_FR_FAIL": "Échec de la mise à jour du fragment.",
	"MESSAGE_COPY_FR_SUCCESS": "Le fragment a été copié avec succès.",
	"MESSAGE_COPY_FR_FAIL": "Impossible de copier le fragment.",
	"MESSAGE_CREATE_ATTR_SUCCESS": "L\'attribut a été créé avec succès.",
	"MESSAGE_CREATE_ATTR_FAIL": "Échec de la création de l\'attribut.",
	"MESSAGE_UPDATE_ATTR_SUCCESS": "L\'attribut a été mis à jour avec succès.",
	"MESSAGE_UPDATE_ATTR_FAIL": "Échec de la mise à jour de l\'attribut.",
	"MESSAGE_DELETE_ATTR_SUCCESS": "L\'attribut a été supprimé avec succès.",
	"MESSAGE_DELETE_ATTR_FAIL": "Échec de la suppression de l\'attribut.",

	"VALIDATION_MINLEN": "La longueur minimale est de %s1.",
	"VALIDATION_MAXLEN": "La longueur maximale est de %s1",
	"VALIDATION_REGEX": "Format invalide ou caractère invalide.",

	"PUBLISH_STATUS_UNPUBLISHED": "Non publié",
	"PUBLISH_STATUS_PUBLISHED": "Publié",
	"PUBLISH_STATUS_INREVIEW": "En cours de révision"
};

var DICTIONARY_MEDIA = {
	"ROOT": "Médiathèque",
	"LABEL_ITEM": ["élément", "éléments", "éléments", "éléments"],
	"LABEL_ITEM_SELECTED": ["élément sélectionné", "éléments sélectionnés", "éléments sélectionnés", "éléments sélectionnés"],
	"MESSAGE_READFOLDER_FAIL": "Échec de l'obtention du dossier.",
	"MESSAGE_PROPERTIES_FAIL": "Impossible d'obtenir les propriétés du fichier.",
	"MESSAGE_UPLOAD_SUCCESS": "Les fichiers ont été téléversés avec succès.",
	"MESSAGE_UPLOAD_PARIAL_FAIL": "Échec du téléversement de certains fichiers.",
	"MESSAGE_UPLOAD_FAIL": "Échec du téléversement des fichiers.",
	"MESSAGE_MAXIMUM_UPLOAD_SIZE_HINT": "Taille max. fichier %s1",
	"MESSAGE_UPLOAD_DENIED": "Le fichier est trop volumineux ou son type n'est pas autorisé.",
	"MESSAGE_FOLDERCREATE_SUCCESS": "Le dossier a été créé avec succès.",
	"MESSAGE_FOLDERCREATE_FAIL": "Échec de la création du dossier.",
	"MESSAGE_DELETE_SUCCESS": "Les éléments sélectionnés ont été supprimés avec succès.",
	"MESSAGE_DELETE_PARTIAL_FAIL": "Échec de la suppression de partie des éléments sélectionnés.",
	"MESSAGE_DELETE_FAIL": "Échec de la suppression des éléments sélectionnés.",
	"MESSAGE_COMPILE_SUCCESS": "Les schémas ont été compilés et rechargés avec succès.",
	"MESSAGE_COMPILE_FAIL": "Échec de la compilation des schémas.",
};

var DICTIONARY_SCHEMATA = {
	"NAME_NEW_SCHEMA": "nouveau schéma",
	"MESSAGE_CREATE_SUCCESS": "Le schéma a été créé avec succès.",
	"MESSAGE_CREATE_FAIL": "Échec de la création du schéma.",
	"MESSAGE_UPDATE_SUCCESS": "Le schéma a été mis à jour avec succès.",
	"MESSAGE_UPDATE_FAIL": "Échec de la mise à jour du schéma.",
	"MESSAGE_DELETE_SUCCESS": "Le schéma a été supprimé avec succès.",
	"MESSAGE_DELETE_FAIL": "Échec de la suppression du schéma.",
	"MESSAGE_DELETE_IMPOSSIBLE": "Ce schéma ne peut pas être supprimé.",
};

var DICTIONARY_USERS = {
	"MESSAGE_CREATE_SUCCESS": "L\'utilisateur a été créé avec succès.",
	"MESSAGE_CREATE_FAIL": "Échec de la création de l\'utilisateur.",
	"MESSAGE_UPDATE_SUCCESS": "L\'utilisateur a été mis à jour avec succès.",
	"MESSAGE_UPDATE_FAIL": "Échec de la mise à jour de l\'utilisateur.",
	"MESSAGE_DELETE_SUCCESS": "L\'utilisateur a été supprimé avec succès.",
	"MESSAGE_DELETE_FAIL": "Échec de la suppression de l\'utilisateur.",
	"MESSAGE_DELETE_IMPOSSIBLE": "Cet utilisateur ne peut pas être supprimé.",
	"MESSAGE_INVALID_ROLE": "Doit être l\'un des éléments suivants: %s1.",
	"MESSAGE_INVALID_ROLE_UPDATE": "Ne peut pas être modifié ou doit figurer sur l\'un des éléments suivants: %s1.",
};

var DICTIONARY_EVENT_DESTINATIONS = {
	"MESSAGE_CREATE_SUCCESS": "La destination de l'événement a été créée avec succès.",
	"MESSAGE_CREATE_FAIL":    "Échec de la création de la destination de l'événement.",
	"MESSAGE_UPDATE_SUCCESS": "La destination de l'événement a été mise à jour avec succès.",
	"MESSAGE_UPDATE_FAIL":    "Échec de la mise à jour de la destination de l'événement.",
	"MESSAGE_DELETE_SUCCESS": "La destination de l'événement a été supprimée avec succès.",
	"MESSAGE_DELETE_FAIL":    "Échec de la suppression de la destination de l'événement.",
};