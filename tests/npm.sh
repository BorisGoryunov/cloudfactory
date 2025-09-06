sudo apt update
# sudo apt install -y ca-certificates curl gnupg
# curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | sudo gpg --dearmor -o /etc/apt/trusted.gpg.d/nodesource.gpg
# echo "deb [signed-by=/etc/apt/trusted.gpg.d/nodesource.gpg] https://deb.nodesource.com/node_20.x nodistro main" | sudo tee /etc/apt/sources.list.d/nodesource.list
# sudo apt update
# sudo apt install -y nodejs
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.3/install.sh | bash

\. "$HOME/.nvm/nvm.sh"

nvm install 22
npm -v
node -v
npm -v

