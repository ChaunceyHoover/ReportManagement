var app = angular.module('ReportManagerLogin', ['ngMaterial', 'ngRoute', 'ngAnimate', 'angular-loading-bar']);

function accessTokenHttpInterceptor($window) {
	return {
		request: function(config) {
			if (config.url.indexOf('/api') !== -1 && config.url !== '/api/auth/token') {
				config.headers['Authorization'] = 'Bearer ' + $window.localStorage.getItem('token');
			}

			return config;
		},
		response: function(response) {
			if (response.config.url === '/api/auth/token' && response.data.access_token) {
				if (response.status === 200) {
					$window.localStorage.setItem('token', response.data.access_token);
				} else {
					$window.localStorage.setItem('token', null);
				}
			}

			return response;
		}
	};
}
accessTokenHttpInterceptor.$inject = ['$window'];
function httpInterceptorRegistry($httpProvider) {
	$httpProvider.interceptors.push('accessTokenHttpInterceptor');
}
httpInterceptorRegistry.$inject = ['$httpProvider'];
app.config(httpInterceptorRegistry).factory('accessTokenHttpInterceptor', accessTokenHttpInterceptor);
app.config(function($mdGestureProvider) {
	$mdGestureProvider.skipClickHijack();
});
app.config(['$httpProvider', function($httpProvider) {
	if (!$httpProvider.defaults.headers.get) {
		$httpProvider.defaults.headers.get = {};
	}

	$httpProvider.defaults.headers.get['Cache-Control'] = 'no-cache';
	$httpProvider.defaults.headers.get['Pragma'] = 'no-cache';
}]);
app.service('UserInfo', function($http, $window) {
	this.verify = function(callback) {
		$http({
			method: 'GET',
			url: '/api/auth',
		}).then(function(response) {
			callback(response, true);
		}).catch(function(response) {
			callback(response, false);
		});
	}
});

/// ** CONFIGURATION ** ///

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
	var customBlue = $mdThemingProvider.extendPalette('blue', {
		'500': '#608fb4'
	});
	var customRed = $mdThemingProvider.extendPalette('red', {
		'500': '#d35f57'
	});
	var customYellow = $mdThemingProvider.extendPalette('yellow', {
		'500': '#dbce62',
		'contrastDefaultColor': 'light'
	});
	var customGreen = $mdThemingProvider.extendPalette('green', {
		'500': '#528e55'
	});
	var customPurple = $mdThemingProvider.extendPalette('deep-purple', {
		'500': '#6e51a3'
	});
	$mdThemingProvider.definePalette('customBlue', customBlue);
	$mdThemingProvider.definePalette('customYellow', customYellow);
	$mdThemingProvider.definePalette('customRed', customRed);
	$mdThemingProvider.definePalette('customGreen', customGreen);
	$mdThemingProvider.definePalette('customPurple', customPurple);
	// "blue-grey" for grey theme

	$mdThemingProvider.theme('blueTheme')
		.primaryPalette('customBlue')
		.accentPalette('blue')
		.warnPalette('customRed')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.theme('yellowTheme')
		.primaryPalette('customYellow')
		.accentPalette('blue')
		.warnPalette('customRed')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.theme('redTheme')
		.primaryPalette('customRed')
		.accentPalette('blue')
		.warnPalette('customYellow')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.theme('greenTheme')
		.primaryPalette('customGreen')
		.accentPalette('blue')
		.warnPalette('customRed')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.theme('purpleTheme')
		.primaryPalette('customPurple')
		.accentPalette('blue')
		.warnPalette('customRed')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.theme('greyTheme')
		.primaryPalette('blue-grey')
		.accentPalette('blue')
		.warnPalette('customRed')
		.backgroundPalette('blue-grey');

	$mdThemingProvider.setDefaultTheme('blueTheme');
});
// Icon name mapping
app.config(function($mdIconProvider) {
	$mdIconProvider
		.icon('user', '/img/icons/user.svg');
});
// Fix for double request sending
app.config(function($mdGestureProvider) {
	$mdGestureProvider.skipClickHijack();
});

/// ** CONTROLLERS ** ///

// Login Controller
// url: /login
app.controller('LoginCtrl', function($scope, $routeParams, $http, $location, $httpParamSerializerJQLike, $window, UserInfo) {
	$scope.user = { "grant_type": "password" };
	$scope.errorMessage = "";

	// note: this will generate a 401 if user is not logged in
	UserInfo.verify(function(response, success) {
		if (success === true)
			$window.location.href = "/";
	});

	$scope.theme = 'blueTheme';
	$http({
		method: 'GET',
		url: '/api/permission/theme'
	}).then(function(response) {
		$scope.theme = response.data.theme.toLowerCase() + "Theme";
	})

	var getURIParams = function(variable) {
		var query = $window.location.search.substring(1);
		var vars = query.split('&');
		for (var i = 0; i < vars.length; i++) {
			var pair = vars[i].split('=');
			if (decodeURIComponent(pair[0]) == variable) {
				return decodeURIComponent(pair[1]);
			}
		}
		return null;
	};

	var status = getURIParams("status");
	if (status == 1)
		$scope.errorMessage = "Successfully logged out";
	$location.search({});

	$scope.login = function() {
		$scope.errorMessage = "";
		$http({
			method: 'POST',
			url: '/api/auth/token',
			headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
			data: $httpParamSerializerJQLike($scope.user)
		}).then(function(response) {
			UserInfo.verify(function(response, success) {
				console.log($routeParams.redirectTo);
				if (success) {
					if (response.data.active) {
						var redirect = new URL(window.location.href).searchParams.get("redirectTo");
						$window.location.href = redirect != null && redirect.length > 0 ? redirect : '/';
					} else {
						$window.localStorage.setItem('token', null);
						$scope.errorMessage = 'User not active';
					}
				}
			});
		}).catch(function(response) {
			$scope.errorMessage = response.data.error_description;
		});
	};
});