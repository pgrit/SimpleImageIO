const path = require('path');

module.exports = {
  entry: './src/flipviewer.ts',
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: 'ts-loader',
        exclude: /node_modules/,
      },
      {
        test: /\.css$/,
  use: [
    "style-loader",
    {
      loader: "css-loader",
      options: {
        modules: true,
        importLoaders: 1,
        sourceMap: true,
      }
    }
  ]

      },
    ],
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js'],
  },
  output: {
    filename: 'flipbook.js',
    path: path.resolve(__dirname, 'dist'),
    library: {
        name: 'flipbook',
        type: 'var',
    },
  },
};
