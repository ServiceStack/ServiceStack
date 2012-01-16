/// <reference path="base.js" />
/// <reference path="login.js" />
(function (root)
{
	var app = root.App;

	app.UserProfile = app.BaseModel.extend({
		url: "api/profile",
		defaults: {
			id: null,
			userName: null,
			displayName: null,
			profileImageUrl64: null,
			showProfile: null,
			email: null
		},
		initialize: function (opt)
		{
			_.bindAll(this, "authChange", "onChange");

			this.login = opt.login;
			this.login.bind("change:isAuthenticated", this.authChange);
			this.bind("change", this.onChange);
		},
		onChange: function () {
			this.login.set({ displayName: this.get('displayName') });
		},
		authChange: function ()
		{
			if (this.login.get("isAuthenticated"))
				this.fetch();
			else 
				this.clear();
		}
	});

	app.UserProfileView = app.BaseView.extend(
		{
			initialize: function ()
			{
				_.bindAll(this, "render");
				this.model.bind("change", this.render);
				this.$el = $(this.el);
				this.template = _.template($("#template-userprofile").html());
			},
			render: function ()
			{
				this.$el.hide();
				var attrs = this.model.attributes;
				attrs.twitterUserId = attrs.twitterUserId || null;
				attrs.facebookUserId = attrs.facebookUserId || null;
				console.log(attrs);

				var showProfile = attrs.email || attrs.twitterUserId || attrs.facebookUserId;
				if (showProfile)
				{
					var html = this.template(attrs);
					this.$el.html(html);
					this.$el.fadeIn('fast');
				} 
				else
				{
					this.$el.html("");
					this.$el.hide();
				}

				$("#facebook-signin").toggle(!attrs.facebookUserId);
				$("#twitter-signin").toggle(!attrs.twitterUserId);
			}
		});

})(window);
