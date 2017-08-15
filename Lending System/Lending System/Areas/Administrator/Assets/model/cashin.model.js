app.createModel= function () {
    "use strict";

    var self = this;

    // #region MODEL TO CREATE/UPDATE
    self.CashInId = ko.observable();
    self.CashInDate = ko.observable();
    self.UserName = ko.observable();
    self.DateFrom = ko.observable();
    self.DateTo = ko.observable();
    self.Amount = ko.observable();
    // #endregion   

    return self;
};