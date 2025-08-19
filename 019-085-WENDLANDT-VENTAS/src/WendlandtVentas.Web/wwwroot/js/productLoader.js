$(document).ready(function () {
    var currencyType = $('#CurrencyTypeGlobal').val();

    $('#productSelectPreload').select2({
        theme: 'bootstrap4',
        minimumInputLength: 0,
        ajax: {
            url: '/Order/SearchProductsAjax',
            dataType: 'json',
            delay: 250,
            data: function (params) {
                return {
                    currencyType: currencyType,
                    term: params.term || '',
                    page: params.page || 1
                };
            },
            processResults: function (data) {
                // Guardar en caché
                sessionStorage.setItem(`products_${currencyType}`, JSON.stringify(data));
                return data;
            }
        }
    });

    // Pre-disparar para precargar
    $('#productSelectPreload').select2('open');
    $('#productSelectPreload').select2('close');
});