set windows-shell := ["pwsh", "-c"]

[working-directory: 'FlipViewer']
frontend:
  npm install
  npm run build
  echo "Copying .js to python package..."
  cp ./dist/flipbook.js ../PyWrapper/simpleimageio/flipbook.js

