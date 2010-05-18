YAHOO.namespace("ajaxstack");
YAHOO.ajaxstack.CommonUtilsTests = new YAHOO.tool.TestCase({

    name: "CommonUtils Tests",

	//--------------------------------------------- 
	// Setup and tear down 
	//---------------------------------------------

	setUp: function()
	{
	},

	tearDown: function()
	{
	},

	//--------------------------------------------- 
	// Tests 
	//---------------------------------------------

	test_Path_Combine: function() {
	    Assert.areEqual(Path.combine('path', 'to'), 'path/to');
	    Assert.areEqual(Path.combine('path', 'to', 'home'), 'path/to/home');
	}

});

