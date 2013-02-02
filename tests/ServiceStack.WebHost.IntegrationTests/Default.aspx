<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ServiceStack.WebHost.IntegrationTests.Default" %>
<%@ Import Namespace="ServiceStack.Html" %>
<%@ Import Namespace="ServiceStack.Text" %>
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="utf-8">
    <title>Authentication / Validation tests</title>
    <meta name="description" content="">
    <meta name="author" content="">

    <!-- Le HTML5 shim, for IE6-8 support of HTML elements -->
    <!--[if lt IE 9]>
      <script src="http://html5shim.googlecode.com/svn/trunk/html5.js"></script>
    <![endif]-->

    <!-- Le styles -->
    <link href="Content/bootstrap.css" rel="stylesheet" />    <link href="Content/default.css" rel="stylesheet" />    <script src="Content/js/jquery-1.7.1.min.js" type="text/javascript"></script>    <script src="Content/js/underscore.min.js" type="text/javascript"></script>    <script src="Content/js/backbone.min.js" type="text/javascript"></script>    <script src="Content/js/jsonreport.js" type="text/javascript"></script>    <script src="Content/js/ss-validation.js" type="text/javascript"></script>    <script src="Content/js/base.js" type="text/javascript"></script>
    <style type="text/css">
      body {
        padding-top: 60px;
      }
    </style>
    <%=ServiceStack.MiniProfiler.Profiler.RenderIncludes(null, null, null, null, false, null).AsRaw() %>
  </head>

  <body>
    <div class="topbar">      <div class="fill">        <div class="container">          <a class="brand" href="/">Home</a>          <ul class="nav">            <li class="active"><a href="#">Home</a></li>            <li><a href="#about">About</a></li>		  	<li><a href="#contact">Contact</a></li>		  	<li><a href="api/auth/logout">Log Off</a></li>          </ul>        </div>      </div>    </div>
    <div id="login" class="container">    
<h2 id="title">Authentication / Validation tests</h2>

<div class="row">
    <div id="signin" class="span12 columns">
        <form class="span5 columns form-stacked fade-when-loading" action="api/auth/credentials" method="POST">
            <div>
                <fieldset>
                    <legend>Sign In</legend>

                    <div class="clearfix">                        <label for="UserName">Email Address</label>                        <input id="UserName" name="UserName" type="text" value="" />                        <span class="help-inline"></span>                    </div>
                    <div class="clearfix">                        <label for="Password">Password</label>                        <input id="Password" name="Password" type="password" />                        <span class="help-inline"></span>                    </div>
                    <div class="clearfix">                        <div class="input">                            <ul class="inputs-list">                                <li>                                    <label>                                        <input id="RememberMe" name="RememberMe" type="checkbox" value="true" />                                        <span>Remember me</span>                                    </label>                                </li>                            </ul>                        </div>                        <span class="help-block"></span>                    </div>                    
                    <p>
                        <span class="error-summary"></span>
                    </p>
					
                    <p>
						<button class="btn" type="submit">Sign In</button><b data-ajax="loading"></b>
						or <a href="#" data-cmd="toggle:register,signin">register</a>
					</p>
                    
					<div id="facebook-signin">
		                <a href="api/auth/facebook"><img src="Content/img/sign-in-with-facebook.png" alt="Sign-in with Facebook" /></a>
	                </div>
                </fieldset>
            </div>
		</form> 

		<div class="span4 columns">
		    <h3>Become a Member</h3>
		    <p>
			    Lorem ipsum dolor sit amet, 
			    consectetuer adipiscing elit, sed diam 
			    nonummy nibh euismod tincidunt ut 
			    laoreet dolore magna aliquam erat volutpat.
		    </p>
		    <button class="btn large primary" data-cmd="toggle:register,signin">Register Now</button>
	    </div>

    </div>
</div>
<div class="row">
    <div id="register" class="hide span12 columns">
        <form class="span8 columns form-stacked fade-when-loading" action="api/register" method="POST">
            <div>
                <fieldset>
                    <legend>Create your member account</legend>

                    <div class="clearfix">                        <label for="DisplayName">DisplayName</label>                        <input id="DisplayName" name="DisplayName" type="text" value="" />                        <span class="help-inline"></span>                    </div>
                    <div class="clearfix">                        <label for="Email">Email</label>                        <input id="Email" name="Email" type="text" value="" />                        <span class="help-inline"></span>                    </div>
                    <div class="clearfix">                        <label for="Password">Password</label>                        <input id="Password" name="Password" type="password" />                        <span class="help-inline"></span>                    </div>
                    <div class="clearfix">                        <label for="ConfirmPassword">ConfirmPassword</label>                        <input id="ConfirmPassword" name="ConfirmPassword" type="password" />                        <span class="help-inline"></span>                    </div>
                 
                    <p>
                        <small>By registering you agree to the <a href="#">Terms & Conditions</a></small>
                    </p>
				
                    <span class="error-summary"></span>

                    <p>
                        <button class="btn" type="submit">Sign Up</button><b data-ajax="loading"></b>
                        or <a href="#" data-cmd="toggle:signin,register">sign in</a>
                    </p>
                </fieldset>
            </div>
        </form> 
    </div>
</div>

<p></p>

<div class="alert-message success hide"><a class="close" href="#">×</a><p></p></div>

<h3>Session Info</h3>

<% if (UserSession.IsAuthenticated) { %>
	<div class="alert-message success">
		<a class="close" href="#">×</a>
		<p><strong>Contgratulations!</strong> You are authenticated</p>
	</div>	
<% } %>

<pre>
<%= UserSession.Dump() %>
</pre>

<div id="userauths"></div>
<div id="oAuthProviders"></div>

<script type="text/javascript">
    $.getJSON("api/userauths", function(r) {
        $("#userauths").html(_.jsonreport(r.results));
        $("#oAuthProviders").html(_.jsonreport(r.oAuthProviders));
    });
</script>


<script type="text/javascript">

    _.each({
            UserName: 'as@if.com',
            DisplayName: 'mythz',
            Email: 'as@if.com',
            Password: 'test',
            ConfirmPassword: 'test'
        }, function (val, name) {
            $("[name=" + name + "]").val(val);
        });

    var clear = function () {
        $(".success, .error-summary").hide();
        $(".error").removeClass("error");
        $(".help-inline").html("");
    };

    $("FORM").submit(function (e) {
        e.preventDefault();
        clear();

        var $form = $(this),
            $config = $form.find("#ConfirmPassword");

        if ($config.length) {
            if ($config.val() != $form.find("#Password").val()) {
                _.setFieldError($config, "passwords do not match");
                return;
            }
        }

        _.post({
            form: $form,
            url: $form.attr("action"),
            data: _.formData($form),
            success: function (r) {
                var msg = r.userName
                    ? "<strong>Welcome " + r.userName + "!</strong> your sessionId is <b>" + r.sessionId + "</b>."
                    : r.userId
                        ? "<strong>Welcome</strong> you are user #" + r.userId
                            + "! You should now <a href='#' data-cmd='toggle:signin,register'>sign in</a>."
                        : "";
                if (msg) {
                    $(".success P").html(msg);
                    $(".success").fadeIn();

                    if (r.userId)
                        $("#signin #UserName").val($("#register #Email").val());
                }
            }
        });
    });

    _.cmdHandler({
        toggle: function (show, hide) {
            clear();
            $("#" + hide).hide();
            $("#" + show).fadeIn();
        }
    });
</script>

    <h2>Test Redirect</h2>

        <form action="api/auth/GoogleOpenId" method="POST">
            <p>
				<button class="btn" type="submit">Sign In with Google OpenId</button>
			</p>
        </form>

        <form action="api/auth/OpenId" method="POST">
            <p>
                <input type="text" name="OpenIdUrl" placeholder="e.g. http://{user}.myopenid.com"/>
				<button class="btn" type="submit">Sign In with OpenId</button>
			</p>
        </form>
    
        <form action="api/auth/credentials" method="POST">
            <div>
                <fieldset>
                    <legend>Sign In w/ Redirect</legend>

                    <div class="clearfix">                        <label for="UserName">Email Address</label>                        <input id="Text1" name="UserName" type="text" value="" />                    </div>
                    <div class="clearfix">                        <label for="Password">Password</label>                        <input id="Password1" name="Password" type="password" />                    </div>
                    <div class="clearfix">                        <label for="Continue">Continue</label>                        <input id="Password2" name="Continue" type="text" value="http://google.com" />                    </div>
                    <div class="clearfix">                        <div class="input">                            <ul class="inputs-list">                                <li>                                    <label>                                        <input id="Checkbox1" name="RememberMe" type="checkbox" value="true" />                                        <span>Remember me</span>                                    </label>                                </li>                            </ul>                        </div>                        <span class="help-block"></span>                    </div>                    
                    <p>
                        <span class="error-summary"></span>
                    </p>
					
                    <p>
						<button class="btn" type="submit">Sign In</button>
					</p>
                </fieldset>
            </div>
		</form> 

    <footer>
        <p>&copy; Company 2012</p>
    </footer>

    </div> <!-- /container -->

  </body>
</html>
