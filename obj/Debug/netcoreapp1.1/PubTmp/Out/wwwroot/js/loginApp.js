var app = angular.module('ReportManagerLogin', ['ngMaterial', 'ngRoute', 'ngAnimate']);

// Disable caching (note: only doing this for dev build; prod build will cache)
app.config(['$httpProvider', function ($httpProvider) {
	if (!$httpProvider.defaults.headers.get) {
		$httpProvider.defaults.headers.get = {};
	}

	$httpProvider.defaults.headers.get['Cache-Control'] = 'no-cache';
	$httpProvider.defaults.headers.get['Pragma'] = 'no-cache';
}]);

// Color theming
app.config(function ($mdThemingProvider) {
	$mdThemingProvider.theme('mainTheme')
		.primaryPalette('blue')
		.accentPalette('green')
		.warnPalette('red')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.setDefaultTheme('mainTheme');
});

// Icon name mapping
app.config(function($mdIconProvider) {
	$mdIconProvider
		.icon('user', '/img/icons/user.svg');
});

// Login Controller
// url: /login
app.controller('LoginCtrl', function($scope, $http, $httpParamSerializerJQLike, $window) {
	$scope.user = { "grant_type": "password" };
	$scope.errorMessage = "";

	$scope.login = function() {
		$scope.errorMessage = "";
		$http({
			method: 'POST',
			url: '/api/auth/token',
			headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
			data: $httpParamSerializerJQLike($scope.user)
		}).then(function(response) {
			$window.sessionStorage.accessToken = response.data.access_token;
			$window.location.href = "/index.html";
		}).catch(function() {
			$scope.errorMessage = "Invalid login, please try again.";
		});
	};

	$scope.logout = function() {
		delete $window.sessionStorage.accessToken;
		$window.location.href = "/login";
	};
});