(function (window, $) {
    'use strict';

    function RegistrationForm(options) {
        this.formSelector = options.formSelector;
        this.countrySelector = options.countrySelector;
        this.citySelector = options.citySelector;
        this.citiesEndpoint = options.citiesEndpoint;
        this.siteKey = options.siteKey;
        this.$form = $(this.formSelector);
        this.$country = $(this.countrySelector);
        this.$city = $(this.citySelector);
    }

    RegistrationForm.prototype.bindEvents = function () {
        var self = this;
        this.$country.on('change', function () {
            self.loadCities($(this).val());
        });
        this.$form.on('submit', function (e) {
            e.preventDefault();
            grecaptcha.ready(function () {
                grecaptcha.execute(self.siteKey, { action: 'register' }).then(function (token) {
                    $('#recaptchaToken').val(token);
                    e.currentTarget.submit();
                });
            });
        });
    };

    RegistrationForm.prototype.loadCities = function (country) {
        var self = this;
        $.ajax({
            url: self.citiesEndpoint,
            method: 'GET',
            data: { country: country }
        }).done(function (cities) {
            self.$city.empty();
            if (Array.isArray(cities)) {
                for (var i = 0; i < cities.length; i++) {
                    var c = cities[i];
                    $('<option/>', { value: c, text: c }).appendTo(self.$city);
                }
            }
        });
    };

    var API = {
        init: function (options) {
            var rf = new RegistrationForm(options);
            rf.bindEvents();
            // initial ensure cities align with current country
            rf.loadCities($(options.countrySelector).val());
        }
    };

    window.RailTixRegister = API;

})(window, window.jQuery);


