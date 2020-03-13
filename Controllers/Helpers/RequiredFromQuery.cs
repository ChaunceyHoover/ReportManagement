using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportPortal.Controllers {
	// https://www.strathweb.com/2016/09/required-query-string-parameters-in-asp-net-core-mvc/

	public class RequiredFromQueryActionConstraint : IActionConstraint {
		private readonly String _parameter;

		public RequiredFromQueryActionConstraint(String parameter) => _parameter = parameter;

		public int Order => 999;

		public Boolean Accept(ActionConstraintContext context) {
			if (!context.RouteContext.HttpContext.Request.Query.ContainsKey(_parameter)) {
				return false;
			}

			return true;
		}
	}

	public class RequiredFromQueryAttribute : FromQueryAttribute, IParameterModelConvention {
		public void Apply(ParameterModel parameter) {
			if (parameter.Action.Selectors != null && parameter.Action.Selectors.Any()) {
				parameter.Action.Selectors.Last().ActionConstraints.Add(new RequiredFromQueryActionConstraint(parameter.BindingInfo?.BinderModelName ?? parameter.ParameterName));
			}
		}
	}
}
