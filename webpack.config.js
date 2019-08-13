// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

var path = require("path");
var webpack = require("webpack");

var isProduction = !process.argv.find(v => v.indexOf('webpack-dev-server') !== -1);

module.exports = {
    mode: "development",
    entry: "./src/App.fsproj",
    output: {
        path: path.join(__dirname, "./public"),
        filename: "bundle.js",
    },
    devServer: {
        contentBase: "./public",
        port: 8080,
        hot: true,
        inline: true
    },
    module: {
        rules: [{
            test: /\.fs(x|proj)?$/,
            use: "fable-loader"
        }]
    },
    plugins : isProduction ? [] : [
        new webpack.HotModuleReplacementPlugin()
    ]
}