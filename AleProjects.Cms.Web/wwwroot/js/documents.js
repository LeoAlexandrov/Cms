var vueAppOptions = {

	data() {
		return {
			pageHeight: "",
			drawer: true,
			profile: { name: "", avatar: "/images/empty-avatar.png" },
			navmenu: [],
			activeNavSection: "documents",
			splitter: 25,

			docTree: [],
			selectedDoc: 0,
			selectedFragment: 0,

			editedDoc: {
				properties: {
					id: 0,
					parent: 0,
					slug: null,
					path: null,
					title: null,
					language: null,
					icon: null,
					tags: null,
					position: null,
					summary: null,
					coverPicture: null,
					description: null,
					editorRoleRequired: null,
					authPolicies: null,
					published: false,
					author: null,
					createdAt: null,
					modifiedAt: null
				},
				fragmentLinks: [],
				fragmentsTree: [],
				attributes: [],
				references: [],
				referencedBy: []
			},

			hasChanged: false,
			invalidDocSlugs: [],
			invalidPublishedState: null,

			newDocumentProps: false,
			newDocument: {
				parent: 0,
				slug: null,
				title: null,
				copyAttributes: false
			},
			invalidNewDocSlugs: [],

			newParentProps: false,
			newParent: 0,
			invalidParents: [],

			deleteDocumentConfirm: false,
			lockDocumentConfirm: false,
			unlockDocumentConfirm: false,
			unsavedDocConfirm: false,
			unsavedDocAction: null,
			deleteFragmentConfirm: false,

			fragmentStuff: {
				templates: [],
				shared: []
			},

			newFragmentProps: false,
			newFragment: {
				parent: 0,
				name: null,
				fragmentId: 0,
				stuffSelected: "new",
				templateSelected: null,
				sharedFragmentSelected: null
			},

			fragmentToDelete: 0,

			fragmentProps: false,
			fragmentPropsTab: "content",
			fragment: {
				properties: {
					id: 0,
					name: null,
					shared: false,
					xmlSchema: null
				},
				linkId: 0,
				containerRef: 0,
				enabled: true,
				anchor: false,
				lockShare: false,
				attributes: [],
				decomposition: [],
				rawXml: null
			},

			deleteFragmentElementConfirm: false,
			fragmentElementToDelete: null,

			newContainer: 0,
			newContainerProps: false,
			invalidContainers: [],


			attributeProps: false,
			attribute: {
				id: 0,
				attributeKey: "new-key",
				value: null,
				enabled: true,
				private: false,
				forFragment: false
			},

			invalidAttributeKeys: [],

			deleteAttributeConfirm: false,

			htmlToolbar: [
				[
					{
						icon: 'format_color_text',
						list: 'no-icons',
						options: ['p', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'code']
					},
					'removeFormat'
				],
				['bold', 'italic', 'underline'],
				[
					{
						icon: 'format_align_left',
						fixedLabel: true,
						list: 'only-icons',
						options: ['left', 'center', 'right', 'justify']
					}
				],
				['token', 'hr', 'link', 'custom_btn', 'quote', 'unordered', 'ordered', 'outdent', 'indent'],
				['undo'],
				['fullscreen', 'viewsource']
			]
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

		imageUrl(link) {
			if (/^\^\('[a-zA-Z0-9+/%]+'\)$/i.test(link)) {
				return `/api/v1/media/entry?link=${link.slice(3, -2)}`;
			}

			return link;
		},

		openImage(link) {
			window.open(this.imageUrl(link), '_blank', 'popup');
		},

		getDocTree(id) {

			Quasar.LoadingBar.start();

			application
				.apiCallAsync("/api/v1/documents/tree", "GET", null, { "Accept": "application/x-msgpack" }, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {
						this.docTree = r.result;

						Vue.nextTick(() => {

							if (id) {

								let node = this.$refs.DocTree.getNodeByKey(id);

								while (node && node.parent) {
									this.$refs.DocTree.setExpanded(node.parent, true);
									node = this.$refs.DocTree.getNodeByKey(node.parent);
								}

								this.selectedDoc = id;

							} else if (r.result.length > 0) {
								this.$refs.DocTree.setExpanded(r.result[0].id, true);
							}
						});
					}
				});
		},

		getEmptyDoc(parentId) {
			return {
				properties: {
					id: 0,
					parent: parentId,
					slug: null,
					title: null,
					language: null,
					icon: "article",
					summary: null,
					coverPicture: null,
					description: null,
					published: true,
					author: this.profile.name,
					createdAt: null,
					modifiedAt: null
				},
				fragmentLinks: [],
				fragmentsTree: [],
				attributes: [],
				references: [],
				referencedBy: []
			};
		},

		loadFragmentsCreationStuff() {

			application
				.apiCallAsync("/api/v1/fragments/creationstuff", "GET", null, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					if (r.ok) {
						this.fragmentStuff = r.result;
					}

				});
		},

		getFragments(id) {
			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/documents/${id}/fragments`, "GET", null, { "Accept": "application/x-msgpack" }, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {
						this.editedDoc.fragmentLinks = r.result.fragmentLinks;
						this.editedDoc.fragmentsTree = r.result.fragmentsTree;
					}
				});
		},

		getRefs(id) {

			application
				.apiCallAsync(`/api/v1/documents/${id}/refs`, "GET", null, { "Accept": "application/x-msgpack" }, null)
				.then((r) => {

					if (r.ok) {
						this.editedDoc.references = r.result.references;
						this.editedDoc.referencedBy = r.result.referencedBy;
					} else {
						this.editedDoc.references = [];
						this.editedDoc.referencedBy = [];
					}
				});

		},

		selectDoc(id, pushState, ignoreChanges, selectNode) {

			if (this.hasChanged && !ignoreChanges) {
				this.confirmUnsavedDoc("select", id);
				return;
			}

			if (id == 0) {

				this.selectedDoc = 0;
				this.selectedFragment = 0;
				this.editedDoc = this.getEmptyDoc(0);
				this.hasChanged = false;
				this.invalidNewDocSlugs = [];
				this.invalidParents = [];
				this.invalidDocSlugs = [];
				this.invalidContainers = [];
				this.invalidAttributeKeys = [];
				this.invalidPublishedState = null;
				this.newFragment.stuffSelected = "new";
				this.newFragment.templateSelected = null;
				this.newFragment.sharedFragmentSelected = null;


				if (pushState)
					window.history.pushState({ docId: 0 }, "", `/documents`);

				return;
			}

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/documents/${id}`, "GET", null, { "Accept": "application/x-msgpack" }, null) 
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.editedDoc = r.result;
						this.editedDoc.references = [];
						this.editedDoc.referencedBy = [];
						this.hasChanged = false;
						this.invalidNewDocSlugs = [];
						this.invalidParents = [];
						this.invalidDocSlugs = [];
						this.invalidContainers = [];
						this.invalidAttributeKeys = [];
						this.invalidPublishedState = null;
						this.selectedFragment = 0;
						this.newFragment.stuffSelected = "new";
						this.newFragment.templateSelected = null;
						this.newFragment.sharedFragmentSelected = null;

						if (selectNode)
							this.selectedDoc = id;

						if (pushState)
							window.history.pushState({ docId: id }, "", `/documents/${id}`);

						this.getRefs(id);

						Vue.nextTick(() => {
							this.$refs.FragmentsTree.expandAll();
						});


					} else {
						displayMessage(`${TEXT.DOCS.get('MESSAGE_OPEN_FAIL')} (${formatHTTPStatus(r)})`, true);

						this.selectedDoc = 0;
						this.selectedFragment = 0;
						this.editedDoc = this.getEmptyDoc(0);
					}
				});
		},

		onDocSelected(id) {

			this.selectDoc(id, true, false, false);

		},

		startNewDoc(parentId, ignoreChanges) {

			if (this.hasChanged && !ignoreChanges) {
				this.confirmUnsavedDoc("create", parentId);
				return;
			}

			this.newDocument = {
				parent: parentId,
				slug: TEXT.DOCS.get('SLUG_NEW_DOCUMENT'),
				title: null,
				copyAttributes: false
			}

			this.newDocumentProps = true;

			Vue.nextTick(() => {
				this.$refs.NewDocumentSlug.focus();
				this.$refs.NewDocumentSlug.select();
			});
		},

		createDoc() {

			let dto = {
				parent: this.newDocument.parent,
				slug: this.newDocument.slug,
				title: this.newDocument.title,
				copyAttributes: this.newDocument.copyAttributes
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/documents`, "POST", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.editedDoc = r.result;
						this.editedDoc.references = [];
						this.editedDoc.referencedBy = [];
						this.hasChanged = false;
						this.newDocumentProps = false;

						let node = {
							id: this.editedDoc.properties.id,
							parent: this.editedDoc.properties.parent,
							label: this.editedDoc.properties.title,
							icon: this.editedDoc.properties.icon,
							iconColor: this.editedDoc.enabled ? "blue-grey" : "blue-grey-2",
							expandable: false,
							selectable: true
						};

						let parent = this.editedDoc.properties.parent;

						if (parent == 0) {

							this.docTree.push(node);

						} else {

							let pNode = this.$refs.DocTree.getNodeByKey(this.editedDoc.properties.parent);

							if (pNode) {
								if (!pNode.hasOwnProperty("children")) {
									pNode["children"] = [node];
									pNode.expandable = true;
								} else if (pNode.children == null) {
									pNode.children = [node];
									pNode.expandable = true;
								} else {
									pNode.children.push(node);
								}
							}

							this.$refs.DocTree.setExpanded(parent, true);
						}

						this.selectedDoc = node.id;

						window.history.pushState({ docId: node.id }, "", `/documents/${node.id}`);

						displayMessage(TEXT.DOCS.get('MESSAGE_CREATE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.DOCS.get('MESSAGE_CREATE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors) {

								if (r.result.errors.Slug)
									this.$refs.NewDocumentSlug.validate();

								if (r.result.errors.Title)
									this.$refs.NewDocumentTitle.validate();

							} else {

								this.$refs.NewDocumentSlug.validate();
								this.$refs.NewDocumentTitle.validate();

							}

						} else if (r.status == 409) {

							this.invalidNewDocSlugs.push(dto.slug);
							this.$refs.NewDocumentSlug.validate();
							this.$refs.NewDocumentSlug.focus();
						}
					}
				});


		},

		updateDoc() {

			if (!this.selectedDoc || !this.hasChanged) {
				this.hasChanged = false;
				return;
			}

			let dto = {
				slug: this.editedDoc.properties.slug,
				title: this.editedDoc.properties.title,
				language: this.editedDoc.properties.language,
				icon: this.editedDoc.properties.icon,
				tags: this.editedDoc.properties.tags,
				summary: this.editedDoc.properties.summary,
				coverPicture: this.editedDoc.properties.coverPicture,
				description: this.editedDoc.properties.description,
				authPolicies: this.editedDoc.properties.authPolicies,
				published: this.editedDoc.properties.published,
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/documents/${this.selectedDoc}`, "PUT", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack" )
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.getRefs(r.result.id);

						this.editedDoc.properties = r.result;
						this.hasChanged = false;

						let node = this.$refs.DocTree.getNodeByKey(this.editedDoc.properties.id);

						if (node) {
							node.label = this.editedDoc.properties.title;
							node.icon = this.editedDoc.properties.icon;

							if (!this.editedDoc.properties.published) {
								iterateNodes(node, (n) => n.iconColor = "blue-grey-2");
							} else {
								node.iconColor = "blue-grey";
							}
						}

						displayMessage(TEXT.DOCS.get('MESSAGE_UPDATE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.DOCS.get('MESSAGE_UPDATE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors) {

								if (r.result.errors.Parent) {

									if (/^[0-9]+$/.test(dto.parent))
										this.invalidParents.push(dto.parent);

									this.$refs.Parent.validate();
								}

								if (r.result.errors.slug)
									this.$refs.Slug.validate();

								if (r.result.errors.Title)
									this.$refs.Title.validate();


								if (r.result.errors.Language)
									this.$refs.Language.validate();

								if (r.result.errors.Icon)
									this.$refs.Icon.validate();

								if (r.result.errors.Published) {
									this.invalidPublishedState = dto.published;
								}

							} else {

								this.$refs.Slug.validate();
								this.$refs.Title.validate();
								this.$refs.Language.validate();
								this.$refs.Icon.validate();

							}

						} else if (r.status == 409) {

							this.invalidDocSlugs.push(dto.slug);
							this.$refs.Slug.validate();
							this.$refs.Slug.focus();
						}

					}
				});

		},

		startChangeParent(ignoreChanges) {

			if (this.hasChanged && !ignoreChanges) {
				this.confirmUnsavedDoc("parent", null);
				return;
			}

			this.newParent = 0;
			this.newParentProps = true;
		},

		changeParent() {

			let dto = {
				parent: this.newParent
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/documents/${this.selectedDoc}/parent`, "POST", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.newParentProps = false;

						if (this.newParent != this.editedDoc.properties.parent) {

							this.editedDoc.properties.parent = r.result.parent;
							this.editedDoc.properties.path = r.result.path;
							this.editedDoc.properties.position = r.result.position;
							this.editedDoc.properties.author = r.result.author;
							this.editedDoc.properties.modifiedAt = r.result.modifiedAt;

							this.getDocTree(this.selectedDoc);
						}

						displayMessage(TEXT.DOCS.get('MESSAGE_UPDATE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.DOCS.get('MESSAGE_UPDATE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400 || r.status == 409) {

							if (r.result.errors) {

								if (r.result.errors.Parent) {

									if (/^[0-9]+$/.test(dto.parent))
										this.invalidParents.push(dto.parent);

									this.$refs.NewParent.validate();
								}

							} else {

								this.$refs.NewParent.validate();

							}

						}

					}
				});

		},

		startDeleteDoc() {
			this.deleteDocumentConfirm = true;
		},

		deleteDoc() {

			this.deleteDocumentConfirm = false;

			let id = this.selectedDoc;
			let parentId = this.editedDoc.properties.parent;
			let parent = parentId == 0 ? null : this.$refs.DocTree.getNodeByKey(parentId);

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/documents/${id}`, "DELETE", null, null, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						if (parent != null) {

							for (let i = 0, n = parent.children.length; i < n; i++)
								if (parent.children[i].id == id) {
									parent.children.splice(i, 1);
									break;
								}

							window.history.replaceState({ docId: parentId }, "", `/documents/${parentId}`);

							this.selectDoc(parentId, false, true, true);

						} else {

							window.history.replaceState({ docId: 0 }, "", `/documents`);

							this.selectDoc(0, false, true, false);

							for (let i = 0, n = this.docTree.length; i < n; i++)
								if (this.docTree[i].id == id) {
									this.docTree.splice(i, 1);
									break;
								}

						}

						displayMessage(TEXT.DOCS.get('MESSAGE_DELETE_SUCCESS'), false);

					} else {
						displayMessage(`${TEXT.DOCS.get('MESSAGE_DELETE_FAIL')} (${formatHTTPStatus(r)})`, true);
					}
				});
		},

		startLockDoc() {
			this.lockDocumentConfirm = true;
		},

		startUnlockDoc() {
			this.unlockDocumentConfirm = true;
		},

		lockDoc(state) {

			this.lockDocumentConfirm = false;
			this.unlockDocumentConfirm = false;

			let msgSuccessId = state ? 'MESSAGE_LOCK_SUCCESS' : 'MESSAGE_UNLOCK_SUCCESS';
			let msgFailId = state ? 'MESSAGE_LOCK_FAIL' : 'MESSAGE_UNLOCK_FAIL';
			let dto = { lockState: state };

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/documents/${this.selectedDoc}/lock`, "POST", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then(r => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.editedDoc.properties.editorRoleRequired = r.result.editorRoleRequired;
						this.editedDoc.properties.author = r.result.author;
						this.editedDoc.properties.modifiedAt = r.result.modifiedAt;

						let node = this.$refs.DocTree.getNodeByKey(this.editedDoc.properties.id);

						if (node) {
							node.label2 = r.result.editorRoleRequired;
						}

						displayMessage(TEXT.DOCS.get(msgSuccessId), false);

					} else {
						displayMessage(`${TEXT.DOCS.get(msgFailId)} (${formatHTTPStatus(r)})`, true);
					}
				});
		},

		moveDoc(increment) {

			let dto = {
				increment: increment
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/documents/${this.selectedDoc}/move`, "POST", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then(r => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						let newPos = r.result.newPosition;
						let oldPos = r.result.oldPosition;

						if (newPos != oldPos) {

							if (this.editedDoc.properties.parent != 0) {

								let pNode = this.$refs.DocTree.getNodeByKey(this.editedDoc.properties.parent);
								let siblings = pNode.children;
								let doc = siblings[oldPos];

								siblings.splice(oldPos, 1);
								pNode.children = [...siblings.slice(0, newPos), doc, ...siblings.slice(newPos)];

							} else {

								let siblings = this.docTree;
								let doc = siblings[oldPos];

								siblings.splice(oldPos, 1);
								this.docTree = [...siblings.slice(0, newPos), doc, ...siblings.slice(newPos)];
							}

							this.editedDoc.properties.position = newPos;
							this.editedDoc.properties.author = r.result.author;
							this.editedDoc.properties.modifiedAt = r.result.modifiedAt;
						}

					} else {

						displayMessage(`${TEXT.DOCS.get('MESSAGE_UPDATE_FAIL')} (${formatHTTPStatus(r)})`, true);

					}
				});

		},

		copyDoc(ignoreChanges) {

			if (this.hasChanged && !ignoreChanges) {
				this.confirmUnsavedDoc("copy", null);
				return;
			}

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/documents/${this.selectedDoc}/copy`, "POST", null, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.editedDoc = r.result;
						this.editedDoc.references = [];
						this.editedDoc.referencedBy = [];
						this.hasChanged = false;

						let node = {
							id: this.editedDoc.properties.id,
							parent: this.editedDoc.properties.parent,
							label: this.editedDoc.properties.title,
							icon: this.editedDoc.properties.icon,
							iconColor: "blue-grey",
							expandable: false,
							selectable: true
						};

						let parent = this.editedDoc.properties.parent;

						if (parent == 0) {

							this.docTree.push(node);

						} else {

							let pNode = this.$refs.DocTree.getNodeByKey(this.editedDoc.properties.parent);

							if (pNode) {
								if (!pNode.hasOwnProperty("children")) {
									pNode["children"] = [node];
									pNode.expandable = true;
								} else if (pNode.children == null) {
									pNode.children = [node];
									pNode.expandable = true;
								} else {
									pNode.children.push(node);
								}
							}

							this.$refs.DocTree.setExpanded(parent, true);
						}

						this.selectedDoc = node.id;

						window.history.pushState({ docId: node.id }, "", `/documents/${node.id}`);

						displayMessage(TEXT.DOCS.get('MESSAGE_CREATE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.DOCS.get('MESSAGE_CREATE_FAIL')} (${formatHTTPStatus(r)})`, true);

					}
				});
		},

		discardDoc() {

			if (this.editedDoc.properties.id) {
				this.hasChanged = false;
				this.selectDoc(this.editedDoc.properties.id, false, true, false);
			} else {
				this.selectDoc(0, false, true, false);
			}

		},

		confirmUnsavedDoc(action, param) {
			this.unsavedDocAction = { action: action, param: param };
			this.unsavedDocConfirm = true;
		},

		confirmStay() {
			this.selectedDoc = this.editedDoc.properties.id;
			this.unsavedDocAction = null;
			this.unsavedDocConfirm = false;
		},

		confirmDiscard() {
			let action = this.unsavedDocAction;

			this.unsavedDocAction = null;
			this.unsavedDocConfirm = false;

			if (action) {
				switch (action.action) {
					case "create":
						this.startNewDoc(action.param, true);
						break;
					case "select":
						this.hasChanged = false;
						this.selectDoc(action.param, true, true, true);
						break;
					case "parent":
						this.startChangeParent(true);
						break;
					case "copy":
						this.copyDoc(true);
						break;
					case "signout":
						this.hasChanged = false;
						this.signout();
						break;
				}
			}
		},

		getFragment(id) {

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/fragments/${id}`, "GET", null, null, null) // { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.invalidContainers = [];

						this.fragmentPropsTab = "content";
						this.fragmentProps = true;
						this.fragment = r.result;

					} else {
						displayMessage(`${TEXT.DOCS.get('MESSAGE_LOAD_FR_FAIL')} (${formatHTTPStatus(r)})`, true);
					}

				});

		},

		startNewFragment(parentId) {

			this.newFragment = {
				parent: parentId,
				name: TEXT.DOCS.get('NAME_NEW_FRAGMENT'),
				fragmentId: 0,
				stuffSelected: "new",
				templateSelected: null,
				sharedFragmentSelected: null
			};

			this.newFragmentProps = true;

			Vue.nextTick(() => {
				this.$refs.NewFragmentName.focus();
				this.$refs.NewFragmentName.select();
			});
		},

		createFragment() {

			let template = null;
			let shared = null;
			let schema = null;

			if (this.newFragment.stuffSelected == "new") {
				template = this.newFragment.templateSelected.value;
				schema = this.newFragment.templateSelected.ns;
			} else {
				shared = this.newFragment.sharedFragmentSelected.value;
				schema = this.newFragment.sharedFragmentSelected.ns;
			}

			let dto = {
				document: this.editedDoc.properties.id,
				parent: this.newFragment.parent,
				name: this.newFragment.name,
				templateName: template,
				sharedFragment: shared,
				schema: schema
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/fragments`, "POST", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.newFragmentProps = false;

						let parent = r.result.link.containerRef;

						let node = {
							id: r.result.link.id,
							parent: parent,
							label: r.result.fragment.name,
							label2: `${r.result.fragment.xmlName} (${r.result.link.id})`,
							data: r.result.link.data,
							icon: r.result.link.icon,
							iconColor: "blue-grey",
							expandable: false,
							selectable: true
						};


						if (parent == 0) {

							this.editedDoc.fragmentLinks.push(r.result.link);
							this.editedDoc.fragmentsTree.push(node);

						} else {

							var pNode = this.$refs.FragmentsTree.getNodeByKey(parent);

							if (pNode) {
								if (!pNode.hasOwnProperty("children")) {
									pNode["children"] = [node];
									pNode.expandable = true;
								} else if (pNode.children == null) {
									pNode.children = [node];
									pNode.expandable = true;
								} else {
									pNode.children.push(node);
								}

								this.$refs.FragmentsTree.setExpanded(parent, true);
							}
						}

						this.selectedFragment = node.id;
						this.editedDoc.properties.author = r.result.author;
						this.editedDoc.properties.modifiedAt = r.result.modifiedAt;

						displayMessage(TEXT.DOCS.get('MESSAGE_CREATE_FR_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.DOCS.get('MESSAGE_CREATE_FR_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors) {

								if (r.result.errors.Name)
									this.$refs.NewFragmentName.validate();

							} else {
								this.$refs.NewFragmentName.validate();
							}
						}
					}
				});

		},

		saveFragment(forceXml, apply) {

			let dto = {
				properties: this.fragment.properties,
				enabled: this.fragment.enabled,
				anchor: this.fragment.anchor,
				linkId: this.fragment.linkId,
				decomposition: forceXml ? null : this.fragment.decomposition,
				rawXml: forceXml ? this.fragment.rawXml : null
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/fragments/${this.fragment.linkId}`, "PUT", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {


						if (!apply)
							this.fragmentProps = false;

						let node = this.$refs.FragmentsTree.getNodeByKey(this.fragment.linkId);

						if (node) {
							node.label = r.result.fragment.name;
							node.iconColor = r.result.link.enabled ? "blue-grey" : "blue-grey-2";
						}

						if (r.result.sharedStateChanged)
							this.loadFragmentsCreationStuff();

						this.getRefs(this.editedDoc.properties.id);
						this.editedDoc.properties.author = r.result.author;
						this.editedDoc.properties.modifiedAt = r.result.modifiedAt;

						displayMessage(TEXT.DOCS.get('MESSAGE_UPDATE_FR_SUCCESS'), false);

					} else {
						displayMessage(`${TEXT.DOCS.get('MESSAGE_UPDATE_FR_FAIL')} (${formatHTTPStatus(r)})`, true);
					}

				});
		},

		startChangeContainer() {
			this.newContainer = 0;
			this.newContainerProps = true;
		},

		changeContainer() {

			let dto = {
				linkId: this.newContainer
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/fragments/${this.fragment.linkId}/container`, "POST", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.newContainerProps = false;

						if (this.newContainer != this.fragment.containerRef) {

							this.fragment.containerRef = this.newContainer;
							this.editedDoc.properties.author = r.result.author;
							this.editedDoc.properties.modifiedAt = r.result.modifiedAt;

							this.getFragments(this.selectedDoc);

							this.selectedFragment = this.fragment.linkId;
						}

						displayMessage(TEXT.DOCS.get('MESSAGE_UPDATE_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.DOCS.get('MESSAGE_UPDATE_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400 || r.status == 409) {

							if (r.result.errors) {

								if (r.result.errors.Container) {

									if (/^[0-9]+$/.test(dto.linkId))
										this.invalidContainers.push(dto.linkId);

									this.$refs.NewContainer.validate();
								}

							} else {

								this.$refs.NewContainer.validate();

							}

						}

					}
				});

		},

		startDeleteFragment(id) {
			this.deleteFragmentConfirm = true;
			this.fragmentToDelete = id;
		},

		deleteFragment() {

			this.deleteFragmentConfirm = false;

			let id = this.fragmentToDelete;

			if (!id)
				return;

			let node = this.$refs.FragmentsTree.getNodeByKey(id);

			if (!node)
				return;

			let parentId = node.parent;
			let parent = parentId == 0 ? null : this.$refs.FragmentsTree.getNodeByKey(parentId);

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/fragments/${id}`, "DELETE", null, null, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						if (parent != null) {

							for (let i = 0, n = parent.children.length; i < n; i++)
								if (parent.children[i].id == id) {
									parent.children.splice(i, 1);
									break;
								}

						} else {

							let tree = this.editedDoc.fragmentsTree;

							for (let i = 0, n = tree.length; i < n; i++)
								if (tree[i].id == id) {
									tree.splice(i, 1);
									break;
								}

						}

						this.getRefs(this.editedDoc.properties.id);
						this.editedDoc.properties.author = r.result.author;
						this.editedDoc.properties.modifiedAt = r.result.modifiedAt;

						displayMessage(TEXT.DOCS.get('MESSAGE_DELETE_FR_SUCCESS'), false);

					} else {
						displayMessage(`${TEXT.DOCS.get('MESSAGE_DELETE_FR_FAIL')} (${formatHTTPStatus(r)})`, true);
					}

				});
		},

		moveFragment(id, parent, increment) {

			let dto = {
				increment: increment
			};

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/fragments/${id}/move`, "POST", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then(r => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						let newPos = r.result.newPosition;
						let oldPos = r.result.oldPosition;

						if (newPos != oldPos) {

							if (parent) {

								let pNode = this.$refs.FragmentsTree.getNodeByKey(parent);
								let siblings = pNode.children;
								let fr = siblings[oldPos];

								siblings.splice(oldPos, 1);
								pNode.children = [...siblings.slice(0, newPos), fr, ...siblings.slice(newPos)];

							} else {

								let siblings = this.editedDoc.fragmentsTree;
								let fr = siblings[oldPos];

								siblings.splice(oldPos, 1);
								this.editedDoc.fragmentsTree = [...siblings.slice(0, newPos), fr, ...siblings.slice(newPos)];

							}

							this.editedDoc.properties.author = r.result.author;
							this.editedDoc.properties.modifiedAt = r.result.modifiedAt;
						}

					} else {

						displayMessage(`${TEXT.DOCS.get('MESSAGE_UPDATE_FAIL')} (${formatHTTPStatus(r)})`, true);

					}
				});
		},

		copyFragment(id) {

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(`/api/v1/fragments/${id}/copy`, "POST", null, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then(r => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						let parent = r.result.link.containerRef;

						let node = {
							id: r.result.link.id,
							parent: parent,
							label: r.result.fragment.name,
							label2: `${r.result.fragment.xmlName} (${r.result.link.id})`,
							data: r.result.link.data,
							icon: r.result.link.icon,
							iconColor: "blue-grey",
							expandable: false,
							selectable: true
						};


						if (parent == 0) {

							this.editedDoc.fragmentLinks.push(r.result.link);
							this.editedDoc.fragmentsTree.push(node);

						} else {

							var pNode = this.$refs.FragmentsTree.getNodeByKey(parent);

							if (pNode) {
								if (!pNode.hasOwnProperty("children")) {
									pNode["children"] = [node];
									pNode.expandable = true;
								} else if (pNode.children == null) {
									pNode.children = [node];
									pNode.expandable = true;
								} else {
									pNode.children.push(node);
								}

								this.$refs.FragmentsTree.setExpanded(parent, true);
							}
						}

						this.editedDoc.properties.author = r.result.author;
						this.editedDoc.properties.modifiedAt = r.result.modifiedAt;

						displayMessage(TEXT.DOCS.get('MESSAGE_COPY_FR_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.DOCS.get('MESSAGE_COPY_FR_FAIL')} (${formatHTTPStatus(r)})`, true);

					}
				});

		},

		countSimilarFragmentElements(pos) {

			let n = this.fragment.decomposition.length;
			let de = this.fragment.decomposition[pos];

			if (de.isSimple) {

				let i = pos - 1;

				while (i >= 0 &&
					this.fragment.decomposition[i].level == de.level &&
					this.fragment.decomposition[i].path == de.path) i--;

				let j = pos + 1;

				while (j < n &&
					this.fragment.decomposition[j].level == de.level &&
					this.fragment.decomposition[j].path == de.path) j++;

				return j - i - 1;

			} else {

				let m = 1;

				for (let i = pos + 1; i < n; i++)
					if (this.fragment.decomposition[i].level < de.level ||
						(this.fragment.decomposition[i].level == de.level && this.fragment.decomposition[i].path != de.path)) break;
					else if (this.fragment.decomposition[i].level == de.level && this.fragment.decomposition[i].path == de.path) m++;

				for (let i = pos - 1; i >= 0; i--)
					if (this.fragment.decomposition[i].level < de.level ||
						(this.fragment.decomposition[i].level == de.level && this.fragment.decomposition[i].path != de.path)) break;
					else if (this.fragment.decomposition[i].level == de.level && this.fragment.decomposition[i].path == de.path) m++;

				return m;
			}
		},

		canAddFragmentElement(pos) {

			let n = this.fragment.decomposition.length;

			if (pos >= n)
				return false;

			let de = this.fragment.decomposition[pos];

			if (de.minOccurs >= de.maxOccurs)
				return false;

			return this.countSimilarFragmentElements(pos) < de.maxOccurs;
		},

		canDeleteFragmentElement(pos) {

			let n = this.fragment.decomposition.length;

			if (pos >= n)
				return false;

			let de = this.fragment.decomposition[pos];

			if (de.minOccurs >= de.maxOccurs)
				return false;

			return this.countSimilarFragmentElements(pos) > de.minOccurs;
		},

		addFragmentElement(pos) {

			let n = this.fragment.decomposition.length;

			if (pos >= n)
				return;

			let de = this.fragment.decomposition[pos];

			if (de.isSimple) {

				if (de.isAddable) {
					de.isAddable = false;
				} else {
					let element = { ...de };

					element.value = de.defaultValue;

					if (pos < n - 1) {
						this.fragment.decomposition = [...this.fragment.decomposition.slice(0, pos + 1), element, ...this.fragment.decomposition.slice(pos + 1)];
					} else {
						this.fragment.decomposition.push(element);
					}
				}

			} else {

				Quasar.LoadingBar.start();

				application
					.apiCallAsync(`/api/v1/fragments/newelement/?path=${de.path}`, "GET", { "Accept": "application/x-msgpack" }, "application/x-msgpack")
					.then((r) => {

						Quasar.LoadingBar.stop();

						if (r.ok) {

							console.log(r.result);

							let elements = r.result;

							if (de.isAddable) {

								if (pos < n - 1) {
									this.fragment.decomposition = [...this.fragment.decomposition.slice(0, pos), ...elements, ...this.fragment.decomposition.slice(pos + 1)];
								} else {
									this.fragment.decomposition = [...this.fragment.decomposition.slice(0, pos), ...elements];
								}

							} else {

								pos++;

								while (pos < n && this.fragment.decomposition[pos].level > de.level)
									pos++;

								if (pos < n) {
									this.fragment.decomposition = [...this.fragment.decomposition.slice(0, pos), ...elements, ...this.fragment.decomposition.slice(pos)];
								} else {
									this.fragment.decomposition = [...this.fragment.decomposition, ...elements];
								}
							}

							console.log(this.fragment.decomposition);

						} else {
							displayMessage(`${TEXT.DOCS.get('MESSAGE_DELETE_FAIL')} (${formatHTTPStatus(r)})`, true);
						}

					});

			}

		},

		startDeleteFragmentElement(pos) {
			this.deleteFragmentElementConfirm = true;
			this.fragmentElementToDelete = pos;
		},

		deleteFragmentElement() {

			let pos = this.fragmentElementToDelete;

			this.deleteFragmentElementConfirm = false;
			this.fragmentElementToDelete = null;

			let n = this.fragment.decomposition.length;

			if (pos >= n)
				return;

			let de = this.fragment.decomposition[pos];

			if (de.isSimple) {

				if (de.minOccurs == 0 && this.countSimilarFragmentElements(pos) == 1) {
					de.isAddable = true;
					de.value = de.defaultValue;
				} else if (pos < n - 1) {
					this.fragment.decomposition = [...this.fragment.decomposition.slice(0, pos), ...this.fragment.decomposition.slice(pos + 1)];
				} else {
					this.fragment.decomposition = this.fragment.decomposition.slice(0, pos);
				}

			} else {

				let isLast = de.minOccurs == 0 && this.countSimilarFragmentElements(pos) == 1;
				let k = pos + 1;

				while (k < n && this.fragment.decomposition[k].level > de.level)
					k++;

				if (k < n) {
					if (isLast) {
						de.isAddable = true;
						de.value = de.defaultValue;
						this.fragment.decomposition = [...this.fragment.decomposition.slice(0, pos + 1), ...this.fragment.decomposition.slice(k)];
					} else {
						this.fragment.decomposition = [...this.fragment.decomposition.slice(0, pos), ...this.fragment.decomposition.slice(k)];
					}
				} else if (isLast) {
					de.isAddable = true;
					de.value = de.defaultValue;
					this.fragment.decomposition = this.fragment.decomposition.slice(0, pos + 1);
				} else {
					this.fragment.decomposition = this.fragment.decomposition.slice(0, pos);
				}

			}
		},

		validateFragment(val, f) {

			if (f.facetMinLength) {
				if (val.length < f.facetMinLength)
					return TEXT.DOCS.format('VALIDATION_MINLEN', f.facetMinLength);
			}

			if (f.facetMaxLength) {
				if (val.length > f.facetMaxLength)
					return TEXT.DOCS.format('VALIDATION_MAXLEN', f.facetMaxLength);
			}

			if (f.facetPattern) {

				let re = new RegExp(`^${f.facetPattern}$`);

				if (!re.test(val))
					return TEXT.DOCS.get('VALIDATION_REGEX');
			}

			if (f.xmlType == "integer" || f.xmlType == "int" || f.xmlType == "short" || f.xmlType == "byte") {

				let v = parseInt(val);

				if (isNaN(v))
					return "Invalid number format";

				if (f.facetMinInclusive !== null) {
					if (v < f.facetMinInclusive)
						return `Minimum inclusive value is ${f.facetMinInclusive}`;
				}

				if (f.facetMinExclusive !== null) {
					if (v <= f.facetMinExclusive)
						return `Minimum exclusive value is ${f.facetMinExclusive}`;
				}

				if (f.facetMaxInclusive !== null) {
					if (v > f.facetMaxInclusive)
						return `Maximum inclusive value is ${f.facetMaxInclusive}`;
				}

				if (f.facetMaxExclusive !== null) {
					if (v >= f.facetMaxExclusive)
						return `Maximum exclusive value is ${f.facetMaxExclusive}`;
				}

			}

			if (f.xmlType == "decimal" || f.xmlType == "double" || f.xmlType == "float") {

				let v = parseFloat(val);

				if (isNaN(v))
					return "Invalid number format";

				if (f.facetMinInclusive !== null) {
					if (v < f.facetMinInclusive)
						return `Minimum inclusive value is ${f.facetMinInclusive}`;
				}

				if (f.facetMinExclusive !== null) {
					if (v <= f.facetMinExclusive)
						return `Minimum exclusive value is ${f.facetMinExclusive}`;
				}

				if (f.facetMaxInclusive !== null) {
					if (v > f.facetMaxInclusive)
						return `Maximum inclusive value is ${f.facetMaxInclusive}`;
				}

				if (f.facetMaxExclusive !== null) {
					if (v >= f.facetMaxExclusive)
						return `Maximum exclusive value is ${f.facetMaxExclusive}`;
				}
			}

			return true;
		},

		startNewAttribute(forFragment) {

			this.attribute = { id: 0, key: "new-key", value: null, enabled: true, private: false, forFragment: forFragment };
			this.attributeProps = true;

			Vue.nextTick(() => {
				this.$refs.AttributeKey.focus();
				this.$refs.AttributeKey.select();
			});
		},

		createAttribute() {

			let dto = {
				attributeKey: this.attribute.key,
				value: this.attribute.value,
				enabled: this.attribute.enabled
			};

			const forFragment = this.attribute.forFragment;
			let url;


			if (forFragment) {
				dto.fragmentLinkRef = this.fragment.linkId;
				url = "/api/v1/fragments/attributes";
			} else {
				dto.documentRef = this.selectedDoc;
				dto.private = this.attribute.private;
				url = "/api/v1/documents/attributes";
			}

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(url, "POST", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.attributeProps = false;

						if (forFragment) {
							this.fragment.attributes.push(r.result);
						} else {
							this.editedDoc.attributes.push(r.result);
						}

						this.getRefs(this.editedDoc.properties.id);

						displayMessage(TEXT.DOCS.get('MESSAGE_CREATE_ATTR_SUCCESS'), false);

					} else {

						displayMessage(`${TEXT.DOCS.get('MESSAGE_CREATE_ATTR_FAIL')} (${formatHTTPStatus(r)})`, true);

						if (r.status == 400) {

							if (r.result.errors) {

								if (r.result.errors.AttributeKey)
									this.$refs.AttributeKey.validate();

							} else {

								this.$refs.AttributeKey.validate();

							}

						} else if (r.status == 409) {

							this.invalidAttributeKeys.push(dto.attributeKey);
							this.$refs.AttributeKey.validate();
							this.$refs.AttributeKey.focus();
						}

					}
				});
		},

		startChangeAttribute(a, forFragment) {
			this.attribute = { id: a.id, key: a.attributeKey, value: a.value, enabled: a.enabled, private: a.private, forFragment: forFragment };
			this.attributeProps = true;
		},

		changeAttribute() {

			const id = this.attribute.id;
			const forFragment = this.attribute.forFragment;

			let dto = { value: this.attribute.value, enabled: this.attribute.enabled };
			let url;

			if (forFragment) {
				dto.documentRef = this.selectedDoc;
				url = `/api/v1/fragments/attributes/${id}`;
			} else {
				dto.private = this.attribute.private;
				url = `/api/v1/documents/attributes/${id}`;
			}

			Quasar.LoadingBar.start();

			application
				.apiCallAsync(url, "PUT", dto, { "Accept": "application/x-msgpack" }, "application/x-msgpack")
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.attributeProps = false;

						if (forFragment) {

							for (const a of this.fragment.attributes)
								if (a.id == id) {
									a.value = r.result.value;
									a.enabled = r.result.enabled;
								}

						} else {

							for (const a of this.editedDoc.attributes)
								if (a.id == id) {
									a.value = r.result.value;
									a.enabled = r.result.enabled;
									a.private = r.result.private;
								}
						}

						this.getRefs(this.editedDoc.properties.id);

						displayMessage(TEXT.DOCS.get('MESSAGE_UPDATE_ATTR_SUCCESS'), false);

					} else {
						displayMessage(`${TEXT.DOCS.get('MESSAGE_UPDATE_ATTR_FAIL')} (${formatHTTPStatus(r)})`, true);
					}
				});
		},

		startDeleteAttribute(a, forFragment) {
			this.attribute = { id: a.id, forFragment: forFragment };
			this.deleteAttributeConfirm = true;
		},

		deleteAttribute() {

			const id = this.attribute.id;
			const forFragment = this.attribute.forFragment;

			let url = forFragment ? 
				`/api/v1/fragments/attributes/${id}?documentRef=${this.selectedDoc}` :
				`/api/v1/documents/attributes/${id}`;


			Quasar.LoadingBar.start();

			application
				.apiCallAsync(url, "DELETE", null, { "Accept": "application/x-msgpack" }, null)
				.then((r) => {

					Quasar.LoadingBar.stop();

					if (r.ok) {

						this.deleteAttributeConfirm = false;

						let attrs;

						if (forFragment) {

							attrs = this.fragment.attributes;

							for (let i = 0, n = attrs.length; i < n; i++)
								if (attrs[i].id == id) {
									this.fragment.attributes = [...attrs.slice(0, i), ...attrs.slice(i + 1)];
									break;
								}
						} else {

							attrs = this.editedDoc.attributes;

							for (let i = 0, n = attrs.length; i < n; i++)
								if (attrs[i].id == id) {
									this.editedDoc.attributes = [...attrs.slice(0, i), ...attrs.slice(i + 1)];
									break;
								}
						}

						this.getRefs(this.editedDoc.properties.id);

						displayMessage(TEXT.DOCS.get('MESSAGE_DELETE_ATTR_SUCCESS'), false);

					} else {
						displayMessage(`${TEXT.DOCS.get('MESSAGE_DELETE_ATTR_FAIL')} (${formatHTTPStatus(r)})`, true);
					}
				});

		}
	},

	computed: {

		nameOfFragmentElementToDelete() {

			if (this.fragmentElementToDelete == null)
				return "";

			let n = this.fragment.decomposition.length;

			if (this.fragmentElementToDelete >= n)
				return "";

			return this.fragment.decomposition[this.fragmentElementToDelete].annotation;
		},

		coverPicture() {

			let link = this.editedDoc.properties.coverPicture;

			if (link)
				if (/^\^\('[a-zA-Z0-9+/%]+'\)$/i.test(link)) {
					return `/api/v1/media/entry?link=${link.slice(3, -2)}`;
				} else {
					return link;
				}
			else
				return null;
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

		this.loadFragmentsCreationStuff();

		let id = document.querySelector("#doc_id"); 

		if (id)
			id = JSON.parse(id.innerHTML);

		this.getDocTree(id);

		if (id)
			this.selectDoc(id, true, true, true);
		else 
			window.history.pushState({ docId: 0 }, "", `/documents`);


		window.onpopstate = (e) => {

			var state = e.state;

			if (state == null) {
				window.history.back();
				return;
			}

			if (state.hasOwnProperty("docId")) {
				this.selectDoc(parseInt(state["docId"]), false, true, true);
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


function iterateNodes(node, f) {
	f(node);

	if (node.hasOwnProperty("children")) {
		for (const n of node.children) {
			iterateNodes(n, f);
		}
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
