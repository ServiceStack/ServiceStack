/// <reference path="base.js" />
(function (root)
{
	var app = root.App;

	app.Login = app.BaseModel.extend({
		urlRoot: "api/auth/credentials",
		defaults: {
			isAuthenticated: null,
			hasRegistered: false,
			sessionId: null,
			userId: null,
			displayName: null,
			form: null
		},
		initialize: function ()
		{
			_.bindAll(this, "loginSuccess", "loginError");
		},
		signOut: function ()
		{
			console.log('Login.signOut');
			this.set({ isAuthenticated: false });
		},
		login: function ($form)
		{
			this.$form = $form;
			_.post({
			    form: $form,
			    url: $form.attr("action"), 
			    data: _.formData($form), 
			    success: this.loginSuccess, 
			    error: this.loginError
			});
		},
		loginSuccess: function (r)
		{
			this.$form.removeClass("error");
			this.set({ isAuthenticated: true });
		},
		loginError: function ()
		{
			this.$form.addClass("error");
		}
	});

	app.LoginView = app.BaseView.extend(
		{
			className: "view-login",

			initialize: function ()
			{
				_.bindAll(this, "login", "render");

				this.model.bind("change:isAuthenticated", this.render);
				this.model.bind("change:displayName", this.render);

				this.$("form").submit(this.login);
			},
			login: function (e)
			{
				if (e) e.preventDefault();
				this.model.login(this.$("form"));
			},
			render: function ()
			{
				var isAuth = this.model.get('isAuthenticated');

				$("#signed-out").toggle(!isAuth);
				$("#signed-in").toggle(isAuth);
				$("#signed-in a.dropdown-toggle").html(this.model.get('displayName') || '');
			},
			signOut: function ()
			{
				console.log('LoginView.signOut');
			    var self = this;
			    _.get("api/auth/logout", function() {
			        self.model.set({ isAuthenticated: false });			        
			    });
			}
		}
	);

})(window);
